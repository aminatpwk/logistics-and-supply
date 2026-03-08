using Confluent.Kafka;
using Events.Messaging;
using Events.Orders;
using Microsoft.Extensions.Hosting;
using System.ComponentModel;

namespace LogisticsTracker.Inventory.EventHandler
{
    public class OrderCreatedConsumer : KafkaEventConsumer<OrderCreatedEvent>
    {
        public OrderCreatedConsumer(
            ConsumerConfig config,
            IServiceProvider serviceProvider,
            ILogger<OrderCreatedConsumer> logger,
            params string[] topics)
            : base(config, serviceProvider, logger, topics)
        {
        }
    }
}
