namespace Events.Inventory
{
    public record InventoryReservedEvent : DomainEvent
    {
        public required Guid ReservationId { get; init; }
        public required Guid ProductId { get; init; }
        public required string StockKeepingUnit { get; init; }
        public required Guid OrderId { get; init; }
        public required int Quantity { get; init; }
        public required int RemainingQuantity { get; init; }
        public override string EventType => "InventoryReserved";
    }
}
