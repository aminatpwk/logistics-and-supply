namespace LogisticsTracker.Inventory.Models.DTOs
{
    public record UpdateStockRequest(
    int Quantity,
    StockMovementType MovementType,
    string? Reason = null);
}
