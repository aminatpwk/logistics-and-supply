namespace LogisticsTracker.Inventory.Models.DTOs
{
    public record LowStockItemResponse(
    Guid ProductId,
    string StockKeepingUnit,
    string Name,
    int CurrentQuantity,
    int ReorderPoint,
    int ReorderQuantity,
    int QuantityToOrder);
}
