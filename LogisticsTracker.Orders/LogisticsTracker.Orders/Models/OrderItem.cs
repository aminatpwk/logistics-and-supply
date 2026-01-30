namespace LogisticsTracker.Orders.Models
{
    public record OrderItem(Guid ProductId, string ProductName, string StockKeepingUnit, int Quantity, decimal UnitPrice)
    {
        public decimal LineTotal => Quantity * UnitPrice;
    }
}
