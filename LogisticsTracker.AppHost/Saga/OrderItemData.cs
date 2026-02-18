namespace Saga
{
    public record OrderItemData(
    Guid ProductId,
    string StockKeepingUnit,
    int Quantity,
    decimal UnitPrice);
}
