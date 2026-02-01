namespace LogisticsTracker.Inventory.Models.DTOs
{
    public record CreateInventoryItemRequest(
    Guid ProductId,
    string StockKeepingUnit,
    string Name,
    string Description,
    int InitialQuantity,
    int ReorderPoint,
    int ReorderQuantity,
    string WarehouseLocation,
    decimal UnitPrice);

}
