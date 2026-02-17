using Events.Inventory;
using Events.Messaging;
using LogisticsTracker.Inventory.Models;
using LogisticsTracker.Inventory.Models.DTOs;
using LogisticsTracker.Inventory.Repository;
using LogisticsTracker.Inventory.Validators;
using System.Runtime.CompilerServices;

namespace LogisticsTracker.Inventory.Service
{
    public class InventoryService : IInventoryService
    {
        private readonly IInventoryRepository _repository;
        private readonly TimeProvider _timeProvider;
        private readonly ILogger<InventoryService> _logger;
        private readonly IEventPublisher _eventPublisher;
        private readonly Lock _stockLock = new();

        public InventoryService(IInventoryRepository repository, TimeProvider timeProvider, ILogger<InventoryService> logger, IEventPublisher eventPublisher)
        {
            _repository = repository;
            _timeProvider = timeProvider;
            _logger = logger;
            _eventPublisher = eventPublisher;
        }

        public async Task<Dictionary<Guid, StockCheckResponse>> CheckStockAvailabilityAsync(IEnumerable<(Guid ProductId, int Quantity)> items)
        {
            var results = new Dictionary<Guid, StockCheckResponse>();
            foreach (var (productId, quantity) in items)
            {
                var item = await _repository.GetByProductIdAsync(productId);
                if (item == null)
                {
                    results[productId] = new StockCheckResponse(
                        productId,
                        string.Empty,
                        0,
                        false,
                        "Product not found in inventory");
                    continue;
                }

                var canFulfill = item.CanReserve(quantity);
                var message = canFulfill ? null : $"Insufficient stock. Available: {item.QuantityAvailable}, Requested: {quantity}";

                results[productId] = new StockCheckResponse(
                    productId,
                    item.StockKeepingUnit,
                    item.QuantityAvailable,
                    canFulfill,
                    message
                );
            }
            return results;
        }

        public async Task<InventoryItem> CreateInventoryItemAsync(CreateInventoryItemRequest request, CancellationToken cancellationToken = default)
        {
            var normalizedSku = StockKeepingUnitValidator.NormalizeSku(request.StockKeepingUnit);
            var (isValid, errorMessage) = StockKeepingUnitValidator.ValidateSkuDetailed(normalizedSku);

            if (!isValid)
            {
                throw new ArgumentException($"Invalid stock keeping unit format: {errorMessage}");
            }

            if (await _repository.StockKeepingUnitExistsAsync(normalizedSku, cancellationToken))
            {
                throw new InvalidOperationException($"Stock keeping unit {normalizedSku} already exists.");
            }

            var item = new InventoryItem
            {
                Id = Guid.NewGuid(),
                ProductId = request.ProductId,
                StockKeepingUnit = normalizedSku,
                Name = request.Name,
                Description = request.Description,
                QuantityAvailable = request.InitialQuantity,
                QuantityReserved = 0,
                ReorderPoint = request.ReorderPoint,
                ReorderQuantity = request.ReorderQuantity,
                WarehouseLocation = request.WarehouseLocation,
                UnitPrice = request.UnitPrice,
                CreatedAt = _timeProvider.GetUtcNow()
            };

            var created = await _repository.CreateAsync(item, cancellationToken);

            var createdEvent = new InventoryItemCreatedEvent
            {
                ProductId = item.ProductId,
                StockKeepingUnit = item.StockKeepingUnit,
                ProductName = item.Name,
                InitialQuantity = item.QuantityAvailable,
                UnitPrice = item.UnitPrice,
                WarehouseLocation = item.WarehouseLocation
            };

            await _eventPublisher.PublishAsync(createdEvent, cancellationToken);

            return created;
        }

        public async Task<List<InventoryItemResponse>> GetAllInventoryAsync(CancellationToken cancellationToken = default)
        {
            var items = await _repository.GetAllAsync(cancellationToken);
            return items.Select(InventoryItemResponse.FromInventoryItem).ToList();
        }

        [OverloadResolutionPriority(1)]
        public async Task<InventoryItem?> GetInventoryItemAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            return await _repository.GetByProductIdAsync(productId, cancellationToken);
        }

        [OverloadResolutionPriority(0)]
        public async Task<InventoryItem?> GetInventoryItemAsync(string stockKeepingUnit, CancellationToken cancellationToken = default)
        {
            var normalizedSku = StockKeepingUnitValidator.NormalizeSku(stockKeepingUnit);
            return await _repository.GetByStockKeepingUnitAsync(normalizedSku, cancellationToken);
        }

        public async Task<List<LowStockItemResponse>> GetLowStockItemsAsync(CancellationToken cancellationToken = default)
        {
            var items = await _repository.GetLowStockItemsAsync(cancellationToken);

            return items.Select(i => new LowStockItemResponse(
                i.ProductId,
                i.StockKeepingUnit,
                i.Name,
                i.TotalQuantity,
                i.ReorderPoint,
                i.ReorderQuantity,
                Math.Max(0, i.ReorderPoint + i.ReorderQuantity - i.TotalQuantity)
            )).ToList();
        }

        public async Task<bool> ReleaseReservationAsync(Guid reservationId, CancellationToken cancellationToken = default)
        {
            lock (_stockLock)
            {
                var reservation = _repository.GetReservationAsync(reservationId, cancellationToken).Result;
                if (reservation == null)
                {
                    _logger.LogWarning("Reservation {ReservationId} not found", reservationId);
                    return false;
                }

                if (!reservation.IsActive)
                {
                    _logger.LogWarning("Reservation {ReservationId} is already released", reservationId);
                    return false;
                }

                var item = _repository.GetByProductIdAsync(reservation.ProductId, cancellationToken).Result;
                if (item == null)
                {
                    throw new KeyNotFoundException($"Product {reservation.ProductId} not found.");
                }

                item.QuantityReserved -= reservation.Quantity;
                item.QuantityAvailable += reservation.Quantity;
                item.UpdatedAt = _timeProvider.GetUtcNow();

                _repository.UpdateAsync(item, cancellationToken).Wait();

                var updatedReservation = reservation with { ReleasedAt = _timeProvider.GetUtcNow() };
                _repository.UpdateReservationAsync(updatedReservation, cancellationToken).Wait();

                var releasedEvent = new InventoryReleasedEvent
                {
                    ReservationId = reservationId,
                    ProductId = reservation.ProductId,
                    StockKeepingUnit = reservation.StockKeepingUnit,
                    OrderId = reservation.OrderId,
                    Quantity = reservation.Quantity,
                    NewAvailableQuantity = item.QuantityAvailable
                };

                _eventPublisher.PublishAsync(releasedEvent, cancellationToken).Wait();
                return true;
            }
        }

        public async Task<InventoryReservation> ReserveInventoryAsync(ReserveInventoryRequest request, CancellationToken cancellationToken = default)
        {
            lock (_stockLock)
            {
                var item = _repository.GetByProductIdAsync(request.ProductId, cancellationToken).Result;
                if (item == null)
                {
                    throw new KeyNotFoundException($"Product {request.ProductId} not found in inventory.");
                }

                if (!item.CanReserve(request.Quantity))
                {
                    throw new InvalidOperationException(
                        $"Insufficient stock for reservation. Available: {item.QuantityAvailable}, Requested: {request.Quantity}");
                }

                item.QuantityAvailable -= request.Quantity;
                item.QuantityReserved += request.Quantity;
                item.UpdatedAt = _timeProvider.GetUtcNow();

                _repository.UpdateAsync(item, cancellationToken).Wait();

                var reservation = new InventoryReservation(
                    Guid.NewGuid(),
                    request.ProductId,
                    item.StockKeepingUnit,
                    request.OrderId,
                    request.Quantity,
                    _timeProvider.GetUtcNow()
                );

                var created = _repository.CreateReservationAsync(reservation, cancellationToken).Result;

                var reservedEvent = new InventoryReservedEvent
                {
                    ReservationId = reservation.Id,
                    ProductId = request.ProductId,
                    StockKeepingUnit = item.StockKeepingUnit,
                    OrderId = request.OrderId,
                    Quantity = request.Quantity,
                    RemainingQuantity = item.QuantityAvailable
                };

                _eventPublisher.PublishAsync(reservedEvent, cancellationToken).Wait();

                return created;
            }
        }

        public async Task<InventoryItem> UpdateStockAsync(Guid productId, UpdateStockRequest request, CancellationToken cancellationToken = default)
        {
            lock (_stockLock)
            {
                var item = _repository.GetByProductIdAsync(productId, cancellationToken).Result;
                if (item == null)
                {
                    throw new KeyNotFoundException($"Product {productId} not found in inventory.");
                }

                var previousQuantity = item.QuantityAvailable;
                switch (request.MovementType)
                {
                    case StockMovementType.Receipt:
                    case StockMovementType.Return:
                        item.QuantityAvailable += request.Quantity;
                        break;
                    case StockMovementType.Adjustment:
                        item.QuantityAvailable = request.Quantity;
                        break;
                    case StockMovementType.Shipment:
                    case StockMovementType.Damage:
                        if (item.QuantityAvailable < request.Quantity)
                        {
                            throw new InvalidOperationException(
                                $"Insufficient stock. Available: {item.QuantityAvailable}, Requested: {request.Quantity}");
                        }
                        item.QuantityAvailable -= request.Quantity;
                        break;

                    default:
                        throw new ArgumentException($"Invalid movement type: {request.MovementType}");
                }

                item.UpdatedAt = _timeProvider.GetUtcNow();

                var updated = _repository.UpdateAsync(item, cancellationToken).Result;

                var stockChangedEvent = new StockLevelChangedEvent
                {
                    ProductId = item.ProductId,
                    StockKeepingUnit = item.StockKeepingUnit,
                    PreviousQuantity = previousQuantity,
                    NewQuantity = item.QuantityAvailable,
                    QuantityChanged = item.QuantityAvailable,
                    MovementType = request.MovementType.ToString(),
                    Reason = request.Reason
                };

                _eventPublisher.PublishAsync(stockChangedEvent, cancellationToken).Wait();

                if (item.IsLowStock)
                {
                    var severity = CalculateAlertSeverity(item);

                    var lowStockEvent = new LowStockAlertEvent
                    {
                        ProductId = item.ProductId,
                        StockKeepingUnit = item.StockKeepingUnit,
                        ProductName = item.Name,
                        CurrentQuantity = item.TotalQuantity,
                        ReorderPoint = item.ReorderPoint,
                        ReorderQuantity = item.ReorderQuantity,
                        QuantityToOrder = Math.Max(0, item.ReorderPoint + item.ReorderQuantity - item.TotalQuantity),
                        Severity = severity
                    };

                    _eventPublisher.PublishAsync(lowStockEvent, cancellationToken).Wait();
                    _logger.LogWarning("Low stock alert for {stockKeepingUnit}: {Quantity} units (reorder point: {ReorderPoint})",
                        item.StockKeepingUnit, item.TotalQuantity, item.ReorderPoint);
                }

                return updated;
            }
        }

        #region private methods
        private static AlertSeverity CalculateAlertSeverity(InventoryItem item)
        {
            var percentageOfReorder = (double)item.TotalQuantity / item.ReorderPoint * 100;

            return percentageOfReorder switch
            {
                0 => AlertSeverity.Critical,
                <= 25 => AlertSeverity.High,
                <= 50 => AlertSeverity.Medium,
                _ => AlertSeverity.Low
            };
        }
        #endregion
    }
}
