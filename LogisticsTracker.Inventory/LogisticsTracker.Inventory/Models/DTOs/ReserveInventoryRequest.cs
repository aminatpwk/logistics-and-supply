namespace LogisticsTracker.Inventory.Models.DTOs
{
    public record ReserveInventoryRequest(
    Guid ProductId,
    Guid OrderId,
    int Quantity);
}
