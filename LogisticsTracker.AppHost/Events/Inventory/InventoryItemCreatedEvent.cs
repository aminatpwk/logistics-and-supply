namespace Events.Inventory
{
    public record InventoryItemCreatedEvent : DomainEvent
    {
        public required Guid ProductId { get; init; }
        public required string StockKeepingUnit { get; init; }
        public required string ProductName { get; init; }
        public required int InitialQuantity { get; init; }
        public required decimal UnitPrice { get; init; }
        public required string WarehouseLocation { get; init; }
        public override string EventType => "InventoryItemCreated";
    }
}
