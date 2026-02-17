using Events.Messaging;
using Events.Orders;

namespace LogisticsTracker.Inventory.EventHandler
{
    public class OrderCreatedHandler(ILogger<OrderCreatedHandler> logger) : IEventHandler<OrderCreatedEvent>
    {
        public Task HandleAsync(OrderCreatedEvent domainEvent, CancellationToken cancellationToken = default)
        {
            logger.LogInformation(
                "Order created: {OrderNumber} for customer {CustomerName} with {ItemCount} items, Total: ${TotalAmount}",
                domainEvent.OrderNumber,
                domainEvent.CustomerName,
                domainEvent.Items.Count,
                domainEvent.TotalAmount);

            return Task.CompletedTask;
        }
    }
}
