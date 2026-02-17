namespace Events.Inventory
{
    public record InventoryReleasedEvent : DomainEvent
    {
        public required Guid ReservationId { get; init; }
        public required Guid ProductId { get; init; }
        public required string StockKeepingUnit { get; init; }
        public required Guid OrderId { get; init; }
        public required int Quantity { get; init; }
        public required int NewAvailableQuantity { get; init; }
        public override string EventType => "InventoryReleased";
    }
}
