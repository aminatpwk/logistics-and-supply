namespace Saga.Orders.Payment
{
    public record RefundResult(
    bool Success,
    string? Message = null);
}
