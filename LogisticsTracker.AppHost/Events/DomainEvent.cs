namespace Events
{
    public abstract record DomainEvent : IDomainEvent
    {
        public Guid EventId { get; init; } = Guid.NewGuid();
        public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
        public abstract string EventType { get; }
        public virtual int Version => 1;
    }
}
