namespace LogisticsTracker.Orders.Clients.Records
{
    public record StockCheckItemResult(
    Guid ProductId,
    string StockKeepingUnit,
    int RequestedQuantity,
    int AvailableQuantity,
    bool CanFulfill,
    string? Message = null);
}
