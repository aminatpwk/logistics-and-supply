namespace Saga
{
    public abstract record SagaResult
    {
        private SagaResult() { }

        public sealed record Success : SagaResult
        {
            public required object? Data { get; init; }
        }

        public sealed record Failed : SagaResult
        {
            public required string Reason { get; init; }
            public required string FailedStep { get; init; }
            public bool WasCompensated { get; init; } = true;
        }

        public sealed record CompensationFailed : SagaResult
        {
            public required string Reason { get; init; }
            public required string FailedStep { get; init; }
            public required string CompensationError { get; init; }
        }

        public static Success Succeeded(object? data = null) => new() { Data = data };
        public static Failed FailedAt(string step, string reason, bool wasCompensated = true) =>
            new() { FailedStep = step, Reason = reason, WasCompensated = wasCompensated };
        public static CompensationFailed FailedToCompensate(string step, string reason, string compensationError) =>
            new() { FailedStep = step, Reason = reason, CompensationError = compensationError };

        public bool IsSuccess => this is Success;
        public bool IsFailure => this is Failed or CompensationFailed;
    }

    public abstract record SagaResult<T>
    {
        private SagaResult() { }

        public sealed record Success : SagaResult<T>
        {
            public required T Data { get; init; }
        }

        public sealed record Failed : SagaResult<T>
        {
            public required string Reason { get; init; }
            public required string FailedStep { get; init; }
            public bool WasCompensated { get; init; } = true;
        }

        public sealed record CompensationFailed : SagaResult<T>
        {
            public required string Reason { get; init; }
            public required string FailedStep { get; init; }
            public required string CompensationError { get; init; }
        }

        public static Success Succeeded(T data) => new() { Data = data };
        public static Failed FailedAt(string step, string reason, bool wasCompensated = true) =>
            new() { FailedStep = step, Reason = reason, WasCompensated = wasCompensated };
        public static CompensationFailed FailedToCompensate(string step, string reason, string compensationError) =>
            new() { FailedStep = step, Reason = reason, CompensationError = compensationError };

        public bool IsSuccess => this is Success;
        public bool IsFailure => this is Failed or CompensationFailed;
    }
}
