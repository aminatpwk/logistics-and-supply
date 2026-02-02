using LogisticsTracker.Inventory.Models;
using System.Collections.Concurrent;

namespace LogisticsTracker.Inventory.Repository
{
    public class InMemoryInventoryRepository : IInventoryRepository
    {
        private readonly ConcurrentDictionary<Guid, InventoryItem> _items = new();
        private readonly ConcurrentDictionary<Guid, InventoryReservation> _reservations = new();
        private readonly Lock _inventoryLock = new();

        public Task<InventoryItem> CreateAsync(InventoryItem item, CancellationToken cancellationToken = default)
        {
            if (!_items.TryAdd(item.ProductId, item))
            {
                throw new InvalidOperationException($"Inventory item for product {item.ProductId} already exists.");
            }

            return Task.FromResult(item);
        }

        public Task<InventoryReservation> CreateReservationAsync(InventoryReservation reservation, CancellationToken cancellationToken = default)
        {
            if (!_reservations.TryAdd(reservation.Id, reservation))
            {
                throw new InvalidOperationException($"Reservation {reservation.Id} already exists.");
            }

            return Task.FromResult(reservation);
        }

        public Task<bool> DeleteAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            var removed = _items.TryRemove(productId, out _);
            return Task.FromResult(removed);
        }

        public Task<List<InventoryReservation>> GetActiveReservationsAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            var reservations = _reservations.Values
            .Where(r => r.ProductId == productId && r.IsActive)
            .OrderBy(r => r.ReservedAt)
            .ToList();

            return Task.FromResult(reservations);
        }

        public Task<List<InventoryItem>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var items = _items.Values.OrderBy(i => i.Name).ToList();
            return Task.FromResult(items);
        }

        public Task<InventoryItem?> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            _items.TryGetValue(productId, out var item);
            return Task.FromResult(item);
        }

        public Task<InventoryItem?> GetByStockKeepingUnitAsync(string stockKeepingUnit, CancellationToken cancellationToken = default)
        {
            var item = _items.Values.FirstOrDefault(i => i.StockKeepingUnit.Equals(stockKeepingUnit, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(item);
        }

        public Task<List<InventoryItem>> GetLowStockItemsAsync(CancellationToken cancellationToken = default)
        {
            var lowStockItems = _items.Values
            .Where(i => i.IsLowStock)
            .OrderBy(i => i.TotalQuantity)
            .ToList();

            return Task.FromResult(lowStockItems);
        }

        public Task<InventoryReservation?> GetReservationAsync(Guid reservationId, CancellationToken cancellationToken = default)
        {
            _reservations.TryGetValue(reservationId, out var reservation);
            return Task.FromResult(reservation);
        }

        public Task<bool> StockKeepingUnitExistsAsync(string stockKeepingUnit, CancellationToken cancellationToken = default)
        {
            var exists = _items.Values.Any(i => i.StockKeepingUnit.Equals(stockKeepingUnit, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(exists);
        }

        public Task<InventoryItem> UpdateAsync(InventoryItem item, CancellationToken cancellationToken = default)
        {
            lock (_inventoryLock)
            {
                if (!_items.ContainsKey(item.ProductId))
                {
                    throw new InvalidOperationException($"Inventory item for product {item.ProductId} not found.");
                }

                _items[item.ProductId] = item;
                return Task.FromResult(item);
            }
        }

        public Task<InventoryReservation> UpdateReservationAsync(InventoryReservation reservation, CancellationToken cancellationToken = default)
        {
            lock (_inventoryLock)
            {
                if (!_reservations.ContainsKey(reservation.Id))
                {
                    throw new InvalidOperationException($"Reservation {reservation.Id} not found.");
                }

                _reservations[reservation.Id] = reservation;
                return Task.FromResult(reservation);
            }
        }
    }
}
