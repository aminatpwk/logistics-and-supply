using Events.Inventory;
using Events.Messaging;

namespace LogisticsTracker.Orders.EventHandlers
{
    public class InventoryReleasedHandler(ILogger<InventoryReleasedHandler> logger) : IEventHandler<InventoryReleasedEvent>
    {
        public Task HandleAsync(InventoryReleasedEvent domainEvent, CancellationToken cancellationToken = default)
        {
            logger.LogInformation(
                "Inventory released for order {OrderId}: {Quantity} units of {SKU} (Reservation: {ReservationId})",
                domainEvent.OrderId,
                domainEvent.Quantity,
                domainEvent.StockKeepingUnit,
                domainEvent.ReservationId);

            return Task.CompletedTask;
        }
    }
}
