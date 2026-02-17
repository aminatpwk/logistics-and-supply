namespace Events.Orders
{
    public record OrderShippedEvent : DomainEvent
    {
        public required Guid OrderId { get; init; }
        public required string OrderNumber { get; init; }
        public required string TrackingNumber { get; init; }
        public required string Carrier { get; init; }
        public required DateTimeOffset ShippedAt { get; init; }
        public override string EventType => "OrderShipped";
    }
}
