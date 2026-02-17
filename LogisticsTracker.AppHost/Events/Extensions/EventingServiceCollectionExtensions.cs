using Confluent.Kafka;
using Events.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Events.Extensions
{
    public static class EventingServiceCollectionExtensions
    {
        public static IServiceCollection AddKafkaEventPublisher(this IServiceCollection services, IConfiguration configuration, string? bootstrapServers = null)
        {
            var kafkaBootstrap = bootstrapServers
                ?? configuration["Kafka:BootstrapServers"]
                ?? "localhost:9092";

            services.AddSingleton<IEventPublisher>(sp =>
            {
                var config = new ProducerConfig
                {
                    BootstrapServers = kafkaBootstrap,
                    ClientId = $"{Environment.MachineName}-producer",
                    Acks = Acks.All,
                    EnableIdempotence = true,
                    MaxInFlight = 5,
                    LingerMs = 5,
                    CompressionType = CompressionType.Snappy
                };

                var logger = sp.GetRequiredService<ILogger<KafkaEventPublisher>>();
                var topicPrefix = configuration["Kafka:TopicPrefix"] ?? "logistics";

                return new KafkaEventPublisher(config, logger, topicPrefix);
            });

            return services;
        }

        public static IServiceCollection AddKafkaEventConsumer<TConsumer, TEvent>(this IServiceCollection services, IConfiguration configuration, string groupId, params string[] topics)
            where TConsumer : KafkaEventConsumer<TEvent>, Microsoft.Extensions.Hosting.IHostedService
            where TEvent : IDomainEvent
        {
            var kafkaBootstrap = configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
            services.AddSingleton(sp =>
            {
                var config = new ConsumerConfig
                {
                    BootstrapServers = kafkaBootstrap,
                    GroupId = groupId,
                    ClientId = $"{Environment.MachineName}-{groupId}",
                    AutoOffsetReset = AutoOffsetReset.Earliest,
                    EnableAutoCommit = true,
                    EnableAutoOffsetStore = true,
                    AutoCommitIntervalMs = 5000
                };

                var logger = sp.GetRequiredService<ILogger<TConsumer>>();
                return (TConsumer)Activator.CreateInstance(typeof(TConsumer), config, sp, logger, topics)!;
            });

            services.AddHostedService(sp => sp.GetRequiredService<TConsumer>());

            return services;
        }

        public static IServiceCollection AddEventHandler<THandler, TEvent>(this IServiceCollection services) where THandler : class, IEventHandler<TEvent> where TEvent : IDomainEvent
        {
            services.AddScoped<IEventHandler<TEvent>, THandler>();
            return services;
        }
    }
}
