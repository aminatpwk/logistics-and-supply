using Confluent.Kafka;
using Events.Inventory;
using Events.Messaging;
using Microsoft.Extensions.Hosting;

namespace LogisticsTracker.Orders.EventHandlers
{
    public class InventoryReleasedConsumer : KafkaEventConsumer<InventoryReleasedEvent>, IHostedService
    {
        public InventoryReleasedConsumer(
            ConsumerConfig config,
            IServiceProvider serviceProvider,
            ILogger<InventoryReleasedConsumer> logger,
            params string[] topics) : base(config, serviceProvider, logger, topics)
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
