namespace LogisticsTracker.Orders.Models.DTOs
{
    public record UpdateOrderStatusRequest(
    OrderStatus NewStatus,
    string? Reason = null);
}
