namespace Saga
{
    public record StepResult
    {
        public required bool Success { get; init; }
        public string? Error { get; init; }
        public object? Data { get; init; }
        public Dictionary<string, object> CompensationData { get; init; } = [];

        public static StepResult Succeeded(object? data = null, Dictionary<string, object>? compensationData = null) =>
            new()
            {
                Success = true,
                Data = data,
                CompensationData = compensationData ?? []
            };

        public static StepResult FailedWith(string error) =>
            new()
            {
                Success = false,
                Error = error
            };
    }
}
