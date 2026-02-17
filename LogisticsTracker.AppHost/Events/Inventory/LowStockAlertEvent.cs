namespace Events.Inventory
{
    public record LowStockAlertEvent : DomainEvent
    {
        public required Guid ProductId { get; init; }
        public required string StockKeepingUnit { get; init; }
        public required string ProductName { get; init; }
        public required int CurrentQuantity { get; init; }
        public required int ReorderPoint { get; init; }
        public required int ReorderQuantity { get; init; }
        public required int QuantityToOrder { get; init; }
        public required AlertSeverity Severity { get; init; }
        public override string EventType => "LowStockAlert";
    }
}
