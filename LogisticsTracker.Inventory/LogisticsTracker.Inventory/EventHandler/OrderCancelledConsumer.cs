using Confluent.Kafka;
using Events.Messaging;
using Events.Orders;
using Microsoft.Extensions.Hosting;
using System.ComponentModel;

namespace LogisticsTracker.Inventory.EventHandler
{
    public class OrderCancelledConsumer : KafkaEventConsumer<OrderCancelledEvent>
    {
        public OrderCancelledConsumer(
            ConsumerConfig config,
            IServiceProvider serviceProvider,
            ILogger<OrderCancelledConsumer> logger,
            params string[] topics)
            : base(config, serviceProvider, logger, topics)
        {
        }
    }
}
