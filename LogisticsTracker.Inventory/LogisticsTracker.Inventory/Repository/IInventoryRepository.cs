using LogisticsTracker.Inventory.Models;

namespace LogisticsTracker.Inventory.Repository
{
    public interface IInventoryRepository
    {
        Task<InventoryItem> CreateAsync(InventoryItem item, CancellationToken cancellationToken = default);
        Task<InventoryItem?> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);
        Task<InventoryItem?> GetByStockKeepingUnitAsync(string stockKeepingUnit, CancellationToken cancellationToken = default);
        Task<List<InventoryItem>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<List<InventoryItem>> GetLowStockItemsAsync(CancellationToken cancellationToken = default);
        Task<InventoryItem> UpdateAsync(InventoryItem item, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(Guid productId, CancellationToken cancellationToken = default);
        Task<bool> StockKeepingUnitExistsAsync(string stockKeepingUnit, CancellationToken cancellationToken = default);
        Task<InventoryReservation> CreateReservationAsync(InventoryReservation reservation, CancellationToken cancellationToken = default);
        Task<InventoryReservation?> GetReservationAsync(Guid reservationId, CancellationToken cancellationToken = default);
        Task<List<InventoryReservation>> GetActiveReservationsAsync(Guid productId, CancellationToken cancellationToken = default);
        Task<InventoryReservation> UpdateReservationAsync(InventoryReservation reservation, CancellationToken cancellationToken = default);

    }
}
