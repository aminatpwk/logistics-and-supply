namespace Saga.Orders.Payment
{
    public record PaymentRequest(
    Guid OrderId,
    Guid CustomerId,
    decimal Amount,
    string Currency);
}
