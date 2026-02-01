namespace LogisticsTracker.Inventory.Models
{
    public record StockMovement(
    Guid Id,
    Guid ProductId,
    string StockKeepingUnit,
    StockMovementType Type,
    int Quantity,
    int PreviousQuantity,
    int NewQuantity,
    string? Reason,
    DateTimeOffset MovementDate);

}
