namespace Saga.Orders
{
    public record OrderResult
    {
        public required Guid OrderId { get; init; }
        public required string ConfirmationNumber { get; init; }
        public required Guid PaymentId { get; init; }
        public required List<Guid> ReservationIds { get; init; }
    }
}
