namespace Saga.Extensions
{
    public record PaymentClientConfiguration
    {
        public string? ApiKey { get; set; }
        public string? ApiUrl { get; set; }
        public int TimeoutSeconds { get; set; } = 30; // for testing purposes only, can be changed
    }
}
