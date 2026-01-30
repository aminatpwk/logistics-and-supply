namespace LogisticsTracker.Orders.Models.DTOs
{
    public record CreateOrderRequest(
    Guid CustomerId,
    string CustomerName,
    string CustomerEmail,
    Address ShippingAddress,
    List<OrderItemRequest> Items,
    string? Notes = null);
}
