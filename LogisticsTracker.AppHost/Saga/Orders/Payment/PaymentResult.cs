namespace Saga.Orders.Payment
{
    public record PaymentResult(
    bool Success,
    Guid? PaymentId,
    string? Message = null);
}
