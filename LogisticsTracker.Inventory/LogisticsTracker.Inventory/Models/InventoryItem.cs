namespace LogisticsTracker.Inventory.Models
{
    public partial class InventoryItem
    {
        public Guid Id { get; set; }

        public Guid ProductId { get; set; }

        public string StockKeepingUnit { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public int QuantityAvailable { get; set; }

        public int QuantityReserved { get; set; }

        public int ReorderPoint { get; set; }

        public int ReorderQuantity { get; set; }

        public string WarehouseLocation { get; set; } = string.Empty;

        public decimal UnitPrice { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? UpdatedAt { get; set; }
        public int TotalQuantity => QuantityAvailable + QuantityReserved;
        public bool IsLowStock => TotalQuantity <= ReorderPoint;
        public bool IsOutOfStock => QuantityAvailable <= 0;
        public bool CanReserve(int quantity) => QuantityAvailable >= quantity; //checks if we have enough quantity to reserve

    }
}
