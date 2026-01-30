namespace LogisticsTracker.Orders.Models.DTOs
{
    public record OrderItemRequest(
    Guid ProductId,
    string ProductName,
    string StockKeepingUnit,
    int Quantity,
    decimal UnitPrice);
}
