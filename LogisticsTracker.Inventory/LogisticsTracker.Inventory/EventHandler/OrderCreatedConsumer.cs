using Confluent.Kafka;
using Events.Messaging;
using Events.Orders;
using Microsoft.Extensions.Hosting;
using System.ComponentModel;

namespace LogisticsTracker.Inventory.EventHandler
{
    public class OrderCreatedConsumer : KafkaEventConsumer<OrderCreatedEvent>, IHostedService, IComponent, IDisposable
    {
        public OrderCreatedConsumer(
            ConsumerConfig config,
            IServiceProvider serviceProvider,
            ILogger<OrderCreatedConsumer> logger,
            params string[] topics)
            : base(config, serviceProvider, logger, topics)
        {
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return ExecuteAsync(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Dispose();
            return Task.CompletedTask;
        }
    }
}
