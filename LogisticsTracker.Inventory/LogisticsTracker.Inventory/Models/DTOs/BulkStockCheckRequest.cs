namespace LogisticsTracker.Inventory.Models.DTOs
{
    public record BulkStockCheckRequest(List<StockCheckItem> Items);
}
