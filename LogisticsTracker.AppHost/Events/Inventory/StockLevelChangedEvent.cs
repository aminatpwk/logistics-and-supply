namespace Events.Inventory
{
    public record StockLevelChangedEvent : DomainEvent
    {
        public required Guid ProductId { get; init; }
        public required string StockKeepingUnit { get; init; }
        public required int PreviousQuantity { get; init; }
        public required int NewQuantity { get; init; }
        public required int QuantityChanged { get; init; }
        public required string MovementType { get; init; }
        public string? Reason { get; init; }
        public override string EventType => "StockLevelChanged";
    }
}
