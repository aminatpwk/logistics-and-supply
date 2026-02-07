namespace LogisticsTracker.Orders.Clients.Records
{
    public record StockCheckResult(
    bool AllAvailable,
    List<StockCheckItemResult> Items,
    string? Message = null);
}
