using Microsoft.Extensions.Logging;

namespace Saga.Orders.Payment
{
    public class MockPaymentClient : IPaymentClient
    {
        private readonly ILogger<MockPaymentClient> _logger;
        public double FailureRate { get; set; } = 0.0;

        private readonly Dictionary<Guid, decimal> _payments = new();

        public MockPaymentClient(ILogger<MockPaymentClient> logger)
        {
            _logger = logger;
        }

        public async Task<PaymentResult> ProcessPaymentAsync(
            PaymentRequest request,
            CancellationToken cancellationToken = default)
        {
            await Task.Delay(50, cancellationToken); 

            if (Random.Shared.NextDouble() < FailureRate)
            {
                _logger.LogWarning(
                    "Mock payment DECLINED for order {OrderId} amount {Amount} {Currency}",
                    request.OrderId, request.Amount, request.Currency);
                return new PaymentResult(false, null, "Payment declined by mock provider");
            }

            var paymentId = Guid.NewGuid();
            _payments[paymentId] = request.Amount;
            return new PaymentResult(true, paymentId);
        }

        public async Task<RefundResult> RefundPaymentAsync(
            Guid paymentId,
            decimal amount,
            CancellationToken cancellationToken = default)
        {
            await Task.Delay(50, cancellationToken);

            if (!_payments.ContainsKey(paymentId))
            {
                _logger.LogWarning("Mock refund attempted for unknown payment {PaymentId}", paymentId);
                return new RefundResult(false, $"Payment {paymentId} not found");
            }

            _payments.Remove(paymentId);
            return new RefundResult(true);
        }
    }
}
