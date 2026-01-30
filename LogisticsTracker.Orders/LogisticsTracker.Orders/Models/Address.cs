namespace LogisticsTracker.Orders.Models
{
    public record Address(string Street, string City, string State, string PostalCode, string Country)
    {
        public Address() : this(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty)
        {
        }
    }
}
