namespace LogisticsTracker.Orders.Models.DTOs
{
    public record OrderResponse(
    Guid Id,
    string OrderNumber,
    Guid CustomerId,
    string CustomerName,
    string CustomerEmail,
    Address ShippingAddress,
    List<OrderItem> Items,
    decimal TotalAmount,
    OrderStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    string? Notes)
    {
        public static OrderResponse FromOrder(Order order)
        {
            return new OrderResponse(
                order.Id,
                order.OrderNumber,
                order.CustomerId,
                order.CustomerName,
                order.CustomerEmail,
                order.ShippingAddress,
                order.Items,
                order.TotalAmount,
                order.Status,
                order.CreatedAt,
                order.UpdatedAt,
                order.Notes
            );
        }
    }
}
