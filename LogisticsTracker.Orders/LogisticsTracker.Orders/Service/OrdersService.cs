using LogisticsTracker.Orders.Models;
using LogisticsTracker.Orders.Models.DTOs;
using LogisticsTracker.Orders.Repository;
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

        public OrdersService(IOrderRepository repository, TimeProvider timeProvider, ILogger<OrdersService> logger)
        {
            _repository = repository;
            _timeProvider = timeProvider;
            _logger = logger;
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
            var createdOrder = await _repository.CreateAsync(order, cancellationToken);

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
                throw new InvalidOperationException(
                    $"Invalid status transition from {order.Status} to {request.NewStatus}");
            }

            var oldStatus = order.Status;
            order.Status = request.NewStatus;
            order.UpdatedAt = _timeProvider.GetUtcNow();
            var updatedOrder = await _repository.UpdateAsync(order, cancellationToken);

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

            order.Status = OrderStatus.Cancelled;
            order.UpdatedAt = _timeProvider.GetUtcNow();

            await _repository.UpdateAsync(order, cancellationToken);

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
