namespace Events.Orders
{
    public record OrderConfirmedEvent : DomainEvent
    {
        public required Guid OrderId { get; init; }
        public required string OrderNumber { get; init; }
        public required Guid CustomerId { get; init; }
        public required DateTimeOffset ConfirmedAt { get; init; }
        public override string EventType => "OrderConfirmed";
    }
}
