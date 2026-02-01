namespace LogisticsTracker.Inventory.Models
{
    public record InventoryReservation(
    Guid Id,
    Guid ProductId,
    string StockKeepingUnit,
    Guid OrderId,
    int Quantity,
    DateTimeOffset ReservedAt,
    DateTimeOffset? ReleasedAt = null)
    {
        public bool IsActive => ReleasedAt == null;
    }
}
