using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Events.Messaging
{
    public class KafkaEventPublisher : IEventPublisher, IAsyncDisposable
    {
        private readonly IProducer<string, string> _producer;
        private readonly ILogger<KafkaEventPublisher> _logger;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly string _topicPrefix;

        public KafkaEventPublisher(ProducerConfig config, ILogger<KafkaEventPublisher> logger, string topicPrefix = "logistics")
        {
            _producer = new ProducerBuilder<string, string>(config)
                .SetErrorHandler((_, error) => logger.LogError("Kafka error: {Reason}", error.Reason))
                .Build();
            _logger = logger;
            _topicPrefix = topicPrefix;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = false
            };
        }

        public async Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default) where TEvent : IDomainEvent
        {
            ArgumentNullException.ThrowIfNull(domainEvent);
            try
            {
                var topic = GetTopicName(domainEvent.EventType);
                var key = domainEvent.EventId.ToString();
                var value = JsonSerializer.Serialize(domainEvent, _jsonOptions);
                var message = new Message<string, string>
                {
                    Key = key,
                    Value = value,
                    Headers = CreateHeaders(domainEvent)
                };
                var result = await _producer.ProduceAsync(topic, message, cancellationToken);
            }
            catch (ProduceException<string, string> ex)
            {
                _logger.LogError(ex,
                    "Failed to publish event {EventType} with ID {EventId}: {Error}",
                    domainEvent.EventType,
                    domainEvent.EventId,
                    ex.Error.Reason);
                throw;
            }
        }

        public async Task PublishBatchAsync<TEvent>(IEnumerable<TEvent> events, CancellationToken cancellationToken = default) where TEvent : IDomainEvent
        {
            ArgumentNullException.ThrowIfNull(events);
            var eventsList = events.ToList();
            foreach (var domainEvent in eventsList)
            {
                await PublishAsync(domainEvent, cancellationToken);
            }
        }


        #region private methods
        private string GetTopicName(string eventType)
        {
            var parts = SplitCamelCase(eventType);
            var topic = $"{_topicPrefix}.{string.Join(".", parts).ToLowerInvariant()}";
            return topic;
        }

        private static List<string> SplitCamelCase(string input)
        {
            var result = new List<string>();
            var currentWord = new System.Text.StringBuilder();

            foreach (var c in input)
            {
                if (char.IsUpper(c) && currentWord.Length > 0)
                {
                    result.Add(currentWord.ToString());
                    currentWord.Clear();
                }
                currentWord.Append(c);
            }

            if (currentWord.Length > 0)
            {
                result.Add(currentWord.ToString());
            }

            return result;
        }

        private static Headers CreateHeaders(IDomainEvent domainEvent)
        {
            var headers = new Headers
            {
                { "event-id", System.Text.Encoding.UTF8.GetBytes(domainEvent.EventId.ToString()) },
                { "event-type", System.Text.Encoding.UTF8.GetBytes(domainEvent.EventType) },
                { "event-version", System.Text.Encoding.UTF8.GetBytes(domainEvent.Version.ToString()) },
                { "occurred-at", System.Text.Encoding.UTF8.GetBytes(domainEvent.OccurredAt.ToString("o")) }
            };

            return headers;
        }

        public async ValueTask DisposeAsync()
        {
            _logger.LogInformation("Flushing and disposing Kafka producer");
            _producer.Flush(TimeSpan.FromSeconds(10));
            _producer.Dispose();
            await Task.CompletedTask;
        }
        #endregion
    }
}
