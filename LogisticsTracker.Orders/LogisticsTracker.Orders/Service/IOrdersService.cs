using LogisticsTracker.Orders.Models;
using LogisticsTracker.Orders.Models.DTOs;

namespace LogisticsTracker.Orders.Service
{
    public interface IOrdersService
    {
        Task<Order> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken = default);
        Task<Order?> GetOrderAsync(Guid orderId, CancellationToken cancellationToken = default);
        Task<Order?> GetOrderAsync(string orderNumber, CancellationToken cancellationToken = default);
        Task<PagedResponse<OrderResponse>> GetOrdersAsync(OrderQueryParameters queryParams, CancellationToken cancellationToken = default);
        Task<Order> UpdateOrderStatusAsync(Guid orderId, UpdateOrderStatusRequest request, CancellationToken cancellationToken = default);
        Task<bool> CancelOrderAsync(Guid orderId, CancellationToken cancellationToken = default);
        Task<Dictionary<Guid, bool>> ValidateOrdersAsync(params Guid[] orderIds);
    }
}
