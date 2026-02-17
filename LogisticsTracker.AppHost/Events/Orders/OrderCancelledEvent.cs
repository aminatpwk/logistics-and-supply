namespace Events.Orders
{
    public record OrderCancelledEvent : DomainEvent
    {
        public required Guid OrderId { get; init; }
        public required string OrderNumber { get; init; }
        public required List<Guid> ReservationIds { get; init; }
        public string? CancellationReason { get; init; }
        public override string EventType => "OrderCancelled";
    }
}
