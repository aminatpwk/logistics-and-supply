using Events.Messaging;
using Events.Orders;
using LogisticsTracker.Inventory.Service;

namespace LogisticsTracker.Inventory.EventHandler
{
    public class OrderCancelledHandler(IInventoryService inventoryService, ILogger<OrderCancelledHandler> logger) : IEventHandler<OrderCancelledEvent>
    {
        public async Task HandleAsync(OrderCancelledEvent domainEvent, CancellationToken cancellationToken = default)
        {
            if (!domainEvent.ReservationIds.Any())
            {
                logger.LogInformation("No reservations to release for order {OrderNumber}", domainEvent.OrderNumber);
                return;
            }

            var releasedCount = 0;
            var failedCount = 0;

            foreach (var reservationId in domainEvent.ReservationIds)
            {
                try
                {
                    var released = await inventoryService.ReleaseReservationAsync(reservationId, cancellationToken);
                    if (released)
                    {
                        releasedCount++;
                        logger.LogInformation("Released reservation {ReservationId}", reservationId);
                    }
                    else
                    {
                        failedCount++;
                        logger.LogWarning("Failed to release reservation {ReservationId}", reservationId);
                    }
                }
                catch (Exception ex)
                {
                    failedCount++;
                    logger.LogError(ex, "Error releasing reservation {ReservationId}", reservationId);
                }
            }
        }
    }
}
