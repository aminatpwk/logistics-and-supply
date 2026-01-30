namespace LogisticsTracker.Orders.Models.DTOs
{
    public record OrderQueryParameters(
    int PageNumber = 1,
    int PageSize = 10,
    OrderStatus? Status = null,
    Guid? CustomerId = null,
    DateTimeOffset? FromDate = null,
    DateTimeOffset? ToDate = null);
}
