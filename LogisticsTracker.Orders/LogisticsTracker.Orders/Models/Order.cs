using System.Net;

namespace LogisticsTracker.Orders.Models
{
    //partial class for potential source generator integration in the future
    public partial class Order
    {
        public Guid Id { get; set; }

        public string OrderNumber { get; set; } = string.Empty;

        public Guid CustomerId { get; set; }

        public string CustomerName { get; set; } = string.Empty;

        public string CustomerEmail { get; set; } = string.Empty;

        public Address ShippingAddress { get; set; } = new();

        public List<OrderItem> Items { get; set; } = [];

        public decimal TotalAmount { get; set; }

        public OrderStatus Status { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? UpdatedAt { get; set; }

        public string? Notes { get; set; }

        public void CalculateTotal()
        {
            TotalAmount = Items.Sum(item => item.Quantity * item.UnitPrice);
        }

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(CustomerName) &&
                   !string.IsNullOrWhiteSpace(CustomerEmail) &&
                   Items.Count > 0 &&
                   Items.All(item => item.Quantity > 0 && item.UnitPrice > 0);
        }
    }
}
