namespace LogisticsTracker.Orders.Models
{
    public enum OrderStatus
    {
        Pending = 0,
        Processing = 1,
        Confirmed = 2,
        Shipped = 3,
        Delivered = 4,
        Cancelled = 5
    }
}
