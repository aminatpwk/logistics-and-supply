namespace Saga
{
    public class SagaContext(Guid sagaId)
    {
        public Guid SagaId { get; } = sagaId;
        public DateTimeOffset StartedAt { get; init; } = DateTimeOffset.UtcNow;
        public string? CurrentStep { get; set; }
        public List<string> CompletedSteps { get; } = [];
        public List<string> CompensatedSteps { get; } = [];
        public Dictionary<string, object> CompensationData { get; } = new();
        public Dictionary<string, object> Metadata { get; } = new();
        public void CompleteStep(string stepName, object? compensationData = null)
        {
            CompletedSteps.Add(stepName);
            if (compensationData is not null)
            {
                CompensationData[stepName] = compensationData;
            }
        }
        public void CompensateStep(string stepName)
        {
            CompensatedSteps.Add(stepName);
        }
        public IEnumerable<string> StepsToCompensate() => CompletedSteps.Except(CompensatedSteps).Reverse();
    }
}
