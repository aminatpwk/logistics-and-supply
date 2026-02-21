using Microsoft.Extensions.Logging;

namespace Saga
{
    public interface ISaga<in TContext> where TContext : SagaContext
    {
        Task<SagaResult> ExecuteAsync(TContext context, CancellationToken cancellationToken = default);
    }

    public interface ISaga<in TContext, TResult> where TContext : SagaContext
    {
        Task<SagaResult<TResult>> ExecuteAsync(TContext context, CancellationToken cancellationToken = default);
    }

    public abstract class SagaBase<TContext>(ILogger logger) : ISaga<TContext> where TContext : SagaContext
    {
        protected readonly ILogger Logger = logger;

        public async Task<SagaResult> ExecuteAsync(TContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await ExecuteStepsAsync(context, cancellationToken);
                if (result.IsSuccess)
                {
                    Logger.LogInformation("Saga {SagaId} completed successfully", context.SagaId);
                    return result;
                }

                Logger.LogWarning("Saga {SagaId} failed, attempting compensation", context.SagaId);
                var compensationSuccess = await CompensateAsync(context, cancellationToken);

                if (compensationSuccess)
                {
                    return result; 
                }
                else
                {
                    Logger.LogError("Saga {SagaId} compensation failed - manual intervention required!", context.SagaId);
                    return SagaResult.FailedToCompensate(
                        context.CurrentStep ?? "Unknown",
                        "Saga failed and compensation also failed",
                        "See logs for details");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Saga {SagaId} threw an exception", context.SagaId);
                try
                {
                    await CompensateAsync(context, cancellationToken);
                }
                catch (Exception compEx)
                {
                    Logger.LogCritical(compEx, "Saga {SagaId} compensation threw exception!", context.SagaId);
                }

                return SagaResult.FailedAt(
                    context.CurrentStep ?? "Unknown",
                    ex.Message,
                    wasCompensated: false);
            }
        }


        protected abstract Task<SagaResult> ExecuteStepsAsync(TContext context, CancellationToken cancellationToken);

        protected virtual async Task<bool> CompensateAsync(TContext context, CancellationToken cancellationToken)
        {
            var stepsToCompensate = context.StepsToCompensate().ToList();

            foreach (var step in stepsToCompensate)
            {
                try
                {
                    context.CurrentStep = $"Compensating:{step}";
                    Logger.LogDebug("Compensating step {Step} for saga {SagaId}", step, context.SagaId);

                    var success = await CompensateStepAsync(context, step, cancellationToken);

                    if (!success)
                    {
                        Logger.LogError("Failed to compensate step {Step} for saga {SagaId}", step, context.SagaId);
                        return false;
                    }

                    context.CompensateStep(step);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Exception during compensation of step {Step} for saga {SagaId}",
                        step, context.SagaId);
                    return false;
                }
            }

            return true;
        }

        protected abstract Task<bool> CompensateStepAsync(TContext context, string stepName, CancellationToken cancellationToken);

        protected async Task<SagaResult> ExecuteStepAsync(TContext context, string stepName, Func<Task<StepResult>> stepAction)
        {
            context.CurrentStep = stepName;
            Logger.LogDebug("Executing step {Step} for saga {SagaId}", stepName, context.SagaId);

            var result = await stepAction();

            if (result.Success)
            {
                context.CompleteStep(stepName, result.CompensationData);
                return SagaResult.Succeeded(result.Data);
            }

            Logger.LogWarning("Step {Step} failed for saga {SagaId}: {Error}",
                stepName, context.SagaId, result.Error);

            return SagaResult.FailedAt(stepName, result.Error ?? "Unknown error");
        }
    }
}
