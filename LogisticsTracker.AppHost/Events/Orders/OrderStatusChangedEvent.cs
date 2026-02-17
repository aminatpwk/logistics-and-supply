namespace Events.Orders
{
    public record OrderStatusChangedEvent : DomainEvent
    {
        public required Guid OrderId { get; init; }
        public required string OrderNumber { get; init; }
        public required string PreviousStatus { get; init; }
        public required string NewStatus { get; init; }
        public string? Reason { get; init; }
        public override string EventType => "OrderStatusChanged";
    }
}
