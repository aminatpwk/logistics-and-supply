namespace Events
{
    public interface IDomainEvent
    {
        Guid EventId { get;  }
        DateTimeOffset OccurredAt { get; }
        string EventType { get; }
        int Version { get; }
    }
}
