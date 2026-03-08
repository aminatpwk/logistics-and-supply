using Confluent.Kafka;
using Events.Inventory;
using Events.Messaging;
using Microsoft.Extensions.Hosting;

namespace LogisticsTracker.Orders.EventHandlers
{
    public class InventoryReleasedConsumer : KafkaEventConsumer<InventoryReleasedEvent>
    {
        public InventoryReleasedConsumer(
            ConsumerConfig config,
            IServiceProvider serviceProvider,
            ILogger<InventoryReleasedConsumer> logger,
            params string[] topics) : base(config, serviceProvider, logger, topics)
        {
        }
    }
}
