using LogisticsTracker.Orders.Models;
using LogisticsTracker.Orders.Clients.Records;

namespace LogisticsTracker.Orders.Clients
{
    public interface IInventoryClient
    {
        Task<StockCheckResult> CheckStockAvailabilityAsync(List<OrderItem> items, CancellationToken cancellationToken = default);
        Task<List<ReservationResult>> ReserveInventoryForOrderAsync(Guid orderId, List<OrderItem> items, CancellationToken cancellationToken = default);
        Task<bool> ReleaseOrderReservationsAsync(Guid orderId, List<Guid> reservationIds, CancellationToken cancellationToken = default);
    }
}
