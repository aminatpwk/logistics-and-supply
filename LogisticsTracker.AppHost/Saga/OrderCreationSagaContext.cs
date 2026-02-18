using Events.Orders;

namespace Saga
{
    public class OrderCreationSagaContext(Guid sagaId) : SagaContext(sagaId)
    {
        public required Guid OrderId { get; init; }
        public required Guid CustomerId { get; init; }
        public required List<OrderItemData> Items { get; init; }
        public required decimal TotalAmount { get; init; }
        public List<Guid> ReservationIds { get; set; } = [];
        public Guid? PaymentId { get; set; }
        public string? ConfirmationNumber { get; set; }
    }
}
