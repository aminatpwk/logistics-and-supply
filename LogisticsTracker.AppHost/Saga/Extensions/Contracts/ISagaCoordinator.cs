namespace Saga.Extensions.Contracts
{
    public interface ISagaCoordinator
    {
        Task<SagaResult<TResult>> ExecuteAsync<TContext, TResult>(
        ISaga<TContext, TResult> saga,
        TContext context,
        CancellationToken cancellationToken = default) where TContext : SagaContext;
    }
}
