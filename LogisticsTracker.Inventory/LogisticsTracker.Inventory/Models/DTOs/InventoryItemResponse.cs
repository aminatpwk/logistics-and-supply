namespace LogisticsTracker.Inventory.Models.DTOs
{
    public record InventoryItemResponse(
    Guid Id,
    Guid ProductId,
    string StockKeepingUnit,
    string Name,
    string Description,
    int QuantityAvailable,
    int QuantityReserved,
    int TotalQuantity,
    int ReorderPoint,
    int ReorderQuantity,
    string WarehouseLocation,
    decimal UnitPrice,
    bool IsLowStock,
    bool IsOutOfStock,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt)
    {
        public static InventoryItemResponse FromInventoryItem(InventoryItem item)
        {
            return new InventoryItemResponse(
                item.Id,
                item.ProductId,
                item.StockKeepingUnit,
                item.Name,
                item.Description,
                item.QuantityAvailable,
                item.QuantityReserved,
                item.TotalQuantity,
                item.ReorderPoint,
                item.ReorderQuantity,
                item.WarehouseLocation,
                item.UnitPrice,
                item.IsLowStock,
                item.IsOutOfStock,
                item.CreatedAt,
                item.UpdatedAt
            );
        }

    }
}
