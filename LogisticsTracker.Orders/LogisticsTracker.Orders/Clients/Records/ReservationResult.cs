namespace LogisticsTracker.Orders.Clients.Records
{
    public record ReservationResult(
    Guid ReservationId,
    Guid ProductId,
    string StockKeepingUnit,
    int Quantity,
    bool Success,
    string? Message = null);
}
