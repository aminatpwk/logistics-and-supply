namespace LogisticsTracker.Inventory.Models.DTOs
{
    public record StockCheckResponse(
    Guid ProductId,
    string StockKeepingUnit,
    int QuantityAvailable,
    bool CanFulfill,
    string? Message = null);
}
