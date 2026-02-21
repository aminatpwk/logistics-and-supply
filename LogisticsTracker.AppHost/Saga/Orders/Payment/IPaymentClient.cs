namespace Saga.Orders.Payment
{
    public interface IPaymentClient
    {
        Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request, CancellationToken cancellationToken = default);
        Task<RefundResult> RefundPaymentAsync(Guid paymentId, decimal amount, CancellationToken cancellationToken = default);
    }
}
