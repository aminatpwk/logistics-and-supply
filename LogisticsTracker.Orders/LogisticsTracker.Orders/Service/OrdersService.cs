using Events.Messaging;
using Events.Orders;
using LogisticsTracker.Orders.Clients;
using LogisticsTracker.Orders.Models;
using LogisticsTracker.Orders.Models.DTOs;
using LogisticsTracker.Orders.Repository;
using System;
using System.Runtime.CompilerServices;

namespace LogisticsTracker.Orders.Service
{
    public class OrdersService : IOrdersService
    {
        private readonly IOrderRepository _repository;
        private readonly TimeProvider _timeProvider;
        private readonly ILogger<OrdersService> _logger;
        private readonly Lock _orderNumberLock = new();
        private int _orderNumberSequence = 1000; //for testing purposes only
        private readonly IInventoryClient _inventoryClient;
        private readonly IEventPublisher _eventPublisher;

        public OrdersService(IOrderRepository repository, IInventoryClient inventoryClient, TimeProvider timeProvider, ILogger<OrdersService> logger, IEventPublisher eventPublisher)
        {
            _repository = repository;
            _inventoryClient = inventoryClient;
            _timeProvider = timeProvider;
            _logger = logger;
            _eventPublisher = eventPublisher;
        }

        public async Task<Order> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken = default)
        {
            var order = new Order
            {
                Id = Guid.NewGuid(),
                OrderNumber = GenerateOrderNumber(),
                CustomerId = request.CustomerId,
                CustomerName = request.CustomerName,
                CustomerEmail = request.CustomerEmail,
                ShippingAddress = request.ShippingAddress,
                Items = request.Items.Select(item => new OrderItem(
                    item.ProductId,
                    item.ProductName,
                    item.StockKeepingUnit,
                    item.Quantity,
                    item.UnitPrice
                )).ToList(),
                Status = OrderStatus.Pending,
                CreatedAt = _timeProvider.GetUtcNow(),
                Notes = request.Notes
            };
            order.CalculateTotal();
            if (!order.IsValid())
            {
                throw new InvalidOperationException("Order validation failed. Please ensure all required fields are provided.");
            }
            var stockCheck = await _inventoryClient.CheckStockAvailabilityAsync(order.Items, cancellationToken);
            if (!stockCheck.AllAvailable)
            {
                var unavailableItems = stockCheck.Items.Where(i => !i.CanFulfill).ToList();
                var errorMessage = $"Insufficient inventory for order. Unavailable items: {string.Join(", ", unavailableItems.Select(i => $"{i.StockKeepingUnit} (need {i.RequestedQuantity}, have {i.AvailableQuantity})"))}";
                _logger.LogWarning("Order {OrderNumber} cannot be fulfilled: {Error}", order.OrderNumber, errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            var reservations = await _inventoryClient.ReserveInventoryForOrderAsync(order.Id, order.Items, cancellationToken);
            var failedReservations = reservations.Where(r => !r.Success).ToList();
            if (failedReservations.Any())
            {
                //rollback any successful reservations to prevent orphaned inventory holds
                var successfulReservationIds = reservations.Where(r => r.Success).Select(r => r.ReservationId).ToList();
                if (successfulReservationIds.Any())
                {
                    _logger.LogWarning("Rolling back {Count} successful reservations due to partial failure", successfulReservationIds.Count);
                    await _inventoryClient.ReleaseOrderReservationsAsync(order.Id, successfulReservationIds, cancellationToken);
                }
                var errorMessage = $"Failed to reserve inventory: {string.Join(", ", failedReservations.Select(r => r.Message))}";
                throw new InvalidOperationException(errorMessage);
            }

            order.ReservationIds = reservations.Select(r => r.ReservationId).ToList();
            var createdOrder = await _repository.CreateAsync(order, cancellationToken);

            var orderCreatedEvent = new OrderCreatedEvent
            {
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                CustomerId = order.CustomerId,
                CustomerName = order.CustomerName,
                CustomerEmail = order.CustomerEmail,
                Items = order.Items.Select(i => new OrderItemData(
                    i.ProductId,
                    i.ProductName,
                    i.StockKeepingUnit,
                    i.Quantity,
                    i.UnitPrice
                )).ToList(),
                TotalAmount = order.TotalAmount,
                ReservationIds = order.ReservationIds
            };

            await _eventPublisher.PublishAsync(orderCreatedEvent, cancellationToken);
            return createdOrder;
        }

        [OverloadResolutionPriority(1)]
        public async Task<Order?> GetOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
        {
            return await _repository.GetByIdAsync(orderId, cancellationToken);
        }

        [OverloadResolutionPriority(0)]
        public async Task<Order?> GetOrderAsync(string orderNumber, CancellationToken cancellationToken = default)
        {
            return await _repository.GetByOrderNumberAsync(orderNumber, cancellationToken);
        }

        public async Task<PagedResponse<OrderResponse>> GetOrdersAsync(OrderQueryParameters queryParams, CancellationToken cancellationToken = default)
        {
            var (orders, totalCount) = await _repository.GetAllAsync(queryParams, cancellationToken);
            var orderResponses = orders.Select(OrderResponse.FromOrder).ToList();

            return new PagedResponse<OrderResponse>(
                orderResponses,
                totalCount,
                queryParams.PageNumber,
                queryParams.PageSize
            );
        }

        public async Task<Order> UpdateOrderStatusAsync(Guid orderId,UpdateOrderStatusRequest request,CancellationToken cancellationToken = default)
        {
            var order = await _repository.GetByIdAsync(orderId, cancellationToken);
            if (order == null)
            {
                throw new KeyNotFoundException($"Order with ID {orderId} not found.");
            }
            if (!IsValidStatusTransition(order.Status, request.NewStatus))
            {
                throw new InvalidOperationException($"Invalid status transition from {order.Status} to {request.NewStatus}");
            }

            var oldStatus = order.Status;
            order.Status = request.NewStatus;
            order.UpdatedAt = _timeProvider.GetUtcNow();
            var updatedOrder = await _repository.UpdateAsync(order, cancellationToken);

            var statusChangedEvent = new OrderStatusChangedEvent
            {
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                PreviousStatus = oldStatus.ToString(),
                NewStatus = request.NewStatus.ToString(),
                Reason = request.Reason
            };

            await _eventPublisher.PublishAsync(statusChangedEvent, cancellationToken);

            if (request.NewStatus == OrderStatus.Confirmed)
            {
                var confirmedEvent = new OrderConfirmedEvent
                {
                    OrderId = order.Id,
                    OrderNumber = order.OrderNumber,
                    CustomerId = order.CustomerId,
                    ConfirmedAt = _timeProvider.GetUtcNow()
                };
                await _eventPublisher.PublishAsync(confirmedEvent, cancellationToken);
            }

            return updatedOrder;
        }

        public async Task<bool> CancelOrderAsync(Guid orderId, CancellationToken cancellationToken = default)
        {
            var order = await _repository.GetByIdAsync(orderId, cancellationToken);
            if (order == null)
            {
                return false;
            }
            if (order.Status == OrderStatus.Shipped || order.Status == OrderStatus.Delivered)
            {
                throw new InvalidOperationException("Cannot cancel an order that has been shipped or delivered.");
            }

            if (order.ReservationIds.Any())
            {
                await _inventoryClient.ReleaseOrderReservationsAsync(order.Id, order.ReservationIds, cancellationToken);
            }

            order.Status = OrderStatus.Cancelled;
            order.UpdatedAt = _timeProvider.GetUtcNow();

            await _repository.UpdateAsync(order, cancellationToken);

            var cancelledEvent = new OrderCancelledEvent
            {
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                ReservationIds = order.ReservationIds,
                CancellationReason = "User requested cancellation"
            };

            await _eventPublisher.PublishAsync(cancelledEvent, cancellationToken);

            return true;
        }

        public async Task<Dictionary<Guid, bool>> ValidateOrdersAsync(params Guid[] orderIds)
        {
            var results = new Dictionary<Guid, bool>();

            foreach (var orderId in orderIds)
            {
                var order = await _repository.GetByIdAsync(orderId);
                results[orderId] = order != null && order.IsValid();
            }

            return results;
        }

        #region private methods
        private string GenerateOrderNumber()
        {
            lock (_orderNumberLock)
            {
                var sequence = _orderNumberSequence++;
                var timestamp = _timeProvider.GetUtcNow();
                return $"ORD-{timestamp:yyyyMMdd}-{sequence:D6}";
            }
        }

        private static bool IsValidStatusTransition(OrderStatus current, OrderStatus next)
        {
            return (current, next) switch
            {
                (OrderStatus.Pending, OrderStatus.Processing) => true,
                (OrderStatus.Pending, OrderStatus.Cancelled) => true,

                (OrderStatus.Processing, OrderStatus.Confirmed) => true,
                (OrderStatus.Processing, OrderStatus.Cancelled) => true,

                (OrderStatus.Confirmed, OrderStatus.Shipped) => true,
                (OrderStatus.Confirmed, OrderStatus.Cancelled) => true,

                (OrderStatus.Shipped, OrderStatus.Delivered) => true,

                // Same status is always allowed 
                _ when current == next => true,

                // All other transitions are invalid
                _ => false
            };
        }

        #endregion
    }
}
