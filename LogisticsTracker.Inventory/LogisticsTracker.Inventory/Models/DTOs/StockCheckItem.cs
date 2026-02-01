namespace LogisticsTracker.Inventory.Models.DTOs
{
    public record StockCheckItem(
    Guid ProductId,
    int RequestedQuantity);
}
