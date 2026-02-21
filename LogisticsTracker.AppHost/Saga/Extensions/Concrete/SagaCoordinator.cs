using Saga.Extensions.Contracts;

namespace Saga.Extensions.Concrete
{
    public class SagaCoordinator : ISagaCoordinator
    {
        public async Task<SagaResult<TResult>> ExecuteAsync<TContext, TResult>(
        ISaga<TContext, TResult> saga,
        TContext context,
        CancellationToken cancellationToken = default) where TContext : SagaContext
        {
            return await saga.ExecuteAsync(context, cancellationToken);
        }
    }
}
