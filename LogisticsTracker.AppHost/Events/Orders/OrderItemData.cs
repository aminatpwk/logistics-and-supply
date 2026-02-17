namespace Events.Orders
{
    public record OrderItemData(
        Guid ProductId,
        string ProductName,
        string StockKeepingUnit,
        int Quantity,
        decimal UnitPrice);
}
