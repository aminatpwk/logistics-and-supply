using Confluent.Kafka;
using Events.Inventory;
using Events.Messaging;
using Microsoft.Extensions.Hosting;

namespace LogisticsTracker.Orders.EventHandlers
{
    public class LowStockAlertConsumer : KafkaEventConsumer<LowStockAlertEvent>, IHostedService, IDisposable
    {
        public LowStockAlertConsumer(
        ConsumerConfig config,
        IServiceProvider serviceProvider,
        ILogger<LowStockAlertConsumer> logger,
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
