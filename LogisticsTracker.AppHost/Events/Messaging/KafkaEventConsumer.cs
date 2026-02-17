using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Events.Messaging
{
    public abstract class KafkaEventConsumer<TEvent> : BackgroundWorker where TEvent : IDomainEvent
    {
        private readonly IConsumer<string, string> _consumer;
        private readonly ILogger _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly string[] _topics;

        protected KafkaEventConsumer(ConsumerConfig config, IServiceProvider serviceProvider, ILogger logger, params string[] topics)
        {
            ArgumentNullException.ThrowIfNull(topics);
            if (topics.Length == 0)
            {
                throw new ArgumentException("At least one topic must be specified", nameof(topics));
            }

            _consumer = new ConsumerBuilder<string, string>(config)
                .SetErrorHandler((_, error) => logger.LogError("Kafka consumer error: {Reason}", error.Reason))
                .SetPartitionsAssignedHandler((_, partitions) =>
                    logger.LogInformation("Partitions assigned: {Partitions}",
                        string.Join(", ", partitions.Select(p => $"{p.Topic}[{p.Partition}]"))))
                .Build();

            _serviceProvider = serviceProvider;
            _logger = logger;
            _topics = topics;

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
        }

        protected async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _consumer.Subscribe(_topics);
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var consumeResult = _consumer.Consume(stoppingToken);

                        if (consumeResult?.Message == null)
                        {
                            continue;
                        }

                        await ProcessMessageAsync(consumeResult, stoppingToken);
                    }
                    catch (ConsumeException ex)
                    {
                        _logger.LogError(ex, "Error consuming message: {Error}", ex.Error.Reason);
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogInformation("Consumer operation cancelled");
                        break;
                    }
                }
            }
            finally
            {
                _consumer.Close();
                _logger.LogInformation("Kafka consumer closed");
            }
        }

        private async Task ProcessMessageAsync(ConsumeResult<string, string> result, CancellationToken cancellationToken)
        {
            try
            {
                var domainEvent = JsonSerializer.Deserialize<TEvent>(result.Message.Value, _jsonOptions);
                if (domainEvent == null)
                {
                    _logger.LogWarning("Failed to deserialize message from offset {Offset}", result.Offset);
                    return;
                }

                using var scope = _serviceProvider.CreateScope();
                var handlers = scope.ServiceProvider.GetServices<IEventHandler<TEvent>>();
                var handlersList = handlers.ToList();

                if (handlersList.Count == 0)
                {
                    _logger.LogWarning("No handlers found for event type {EventType}", typeof(TEvent).Name);
                    return;
                }

                foreach (var handler in handlersList)
                {
                    await handler.HandleAsync(domainEvent, cancellationToken);
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize message from offset {Offset}", result.Offset);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error processing message from offset {Offset}: {Error}",
                    result.Offset,
                    ex.Message);
            }
        }

        public void Dispose()
        {
            _consumer?.Dispose();
            base.Dispose();
        }
    }
}
