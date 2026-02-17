namespace Events.Orders
{
    public record OrderCreatedEvent : DomainEvent
    {
        public required Guid OrderId { get; init; }
        public required string OrderNumber { get; init; }
        public required Guid CustomerId { get; init; }
        public required string CustomerName { get; init; }
        public required string CustomerEmail { get; init; }
        public required List<OrderItemData> Items { get; init; }
        public required decimal TotalAmount { get; init; }
        public required List<Guid> ReservationIds { get; init; }
        public override string EventType => "OrderCreated";
    }
}
