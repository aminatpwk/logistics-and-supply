using Microsoft.Extensions.DependencyInjection;
using Saga.Extensions.Concrete;
using Saga.Extensions.Contracts;
using Saga.Orders;

namespace Saga.Extensions
{
    public static class SagaServiceCollectionExtensions
    {
        public static IServiceCollection AddSagas(this IServiceCollection services)
        {
            services.AddScoped<ISagaCoordinator, SagaCoordinator>();
            services.AddScoped<OrderCreationSaga>();

            return services;
        }
    }
}
