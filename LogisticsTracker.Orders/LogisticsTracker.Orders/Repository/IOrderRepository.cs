using LogisticsTracker.Orders.Models;
using LogisticsTracker.Orders.Models.DTOs;

namespace LogisticsTracker.Orders.Repository
{
    public interface IOrderRepository
    {
        Task<Order> CreateAsync(Order order, CancellationToken cancellationToken = default);
        Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default);
        Task<(List<Order> Orders, int TotalCount)> GetAllAsync(OrderQueryParameters queryParams,CancellationToken cancellationToken = default);
        Task<Order> UpdateAsync(Order order, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
        Task<List<Order>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);
        Task<bool> OrderNumberExistsAsync(string orderNumber, CancellationToken cancellationToken = default);
    }
}
