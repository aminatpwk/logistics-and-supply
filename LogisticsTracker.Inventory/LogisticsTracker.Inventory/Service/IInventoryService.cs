using LogisticsTracker.Inventory.Models;
using LogisticsTracker.Inventory.Models.DTOs;

namespace LogisticsTracker.Inventory.Service
{
    public interface IInventoryService
    {
        Task<InventoryItem> CreateInventoryItemAsync(CreateInventoryItemRequest request, CancellationToken cancellationToken = default);
        Task<InventoryItem?> GetInventoryItemAsync(Guid productId, CancellationToken cancellationToken = default);
        Task<InventoryItem?> GetInventoryItemAsync(string stockKeepingUnit, CancellationToken cancellationToken = default);
        Task<List<InventoryItemResponse>> GetAllInventoryAsync(CancellationToken cancellationToken = default);
        Task<List<LowStockItemResponse>> GetLowStockItemsAsync(CancellationToken cancellationToken = default);
        Task<InventoryItem> UpdateStockAsync(Guid productId, UpdateStockRequest request, CancellationToken cancellationToken = default);
        Task<InventoryReservation> ReserveInventoryAsync(ReserveInventoryRequest request, CancellationToken cancellationToken = default);
        Task<bool> ReleaseReservationAsync(Guid reservationId, CancellationToken cancellationToken = default);
        Task<Dictionary<Guid, StockCheckResponse>> CheckStockAvailabilityAsync(IEnumerable<(Guid ProductId, int Quantity)> items);
    }
}
