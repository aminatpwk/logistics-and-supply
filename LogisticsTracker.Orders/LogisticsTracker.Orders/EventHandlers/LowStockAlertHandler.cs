using Events.Inventory;
using Events.Messaging;

namespace LogisticsTracker.Orders.EventHandlers
{
    public class LowStockAlertHandler(ILogger<LowStockAlertHandler> logger) : IEventHandler<LowStockAlertEvent>
    {
        public Task HandleAsync(LowStockAlertEvent domainEvent, CancellationToken cancellationToken = default)
        {
            logger.LogWarning(
                "Low stock alert for {ProductName} ({StockKeepingUnit}): {CurrentQuantity} units remaining (Reorder point: {ReorderPoint}). Severity: {Severity}",
                domainEvent.ProductName,
                domainEvent.StockKeepingUnit,
                domainEvent.CurrentQuantity,
                domainEvent.ReorderPoint,
                domainEvent.Severity);

            return Task.CompletedTask;
        }
    }
}
