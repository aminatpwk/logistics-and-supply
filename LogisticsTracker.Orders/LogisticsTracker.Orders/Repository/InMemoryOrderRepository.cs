using LogisticsTracker.Orders.Models;
using LogisticsTracker.Orders.Models.DTOs;
using System.Collections.Concurrent;

namespace LogisticsTracker.Orders.Repository
{
    public class InMemoryOrderRepository : IOrderRepository
    {
        private readonly Lock _lock = new();
        private readonly ConcurrentDictionary<Guid, Order> _orders = new();

        public Task<Order> CreateAsync(Order order, CancellationToken cancellationToken = default)
        {
            if (!_orders.TryAdd(order.Id, order))
            {
                throw new InvalidOperationException($"Order with ID {order.Id} already exists.");
            }

            return Task.FromResult(order);
        }

        public Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            _orders.TryGetValue(id, out var order);
            return Task.FromResult(order);
        }

        public Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default)
        {
            var order = _orders.Values.FirstOrDefault(o => o.OrderNumber == orderNumber);
            return Task.FromResult(order);
        }

        public Task<(List<Order> Orders, int TotalCount)> GetAllAsync(OrderQueryParameters queryParams, CancellationToken cancellationToken = default)
        {
            var query = _orders.Values.AsQueryable();
            if (queryParams.Status.HasValue)
            {
                query = query.Where(o => o.Status == queryParams.Status.Value);
            }

            if (queryParams.CustomerId.HasValue)
            {
                query = query.Where(o => o.CustomerId == queryParams.CustomerId.Value);
            }

            if (queryParams.FromDate.HasValue)
            {
                query = query.Where(o => o.CreatedAt >= queryParams.FromDate.Value);
            }

            if (queryParams.ToDate.HasValue)
            {
                query = query.Where(o => o.CreatedAt <= queryParams.ToDate.Value);
            }

            var totalCount = query.Count();
            var orders = query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((queryParams.PageNumber - 1) * queryParams.PageSize)
                .Take(queryParams.PageSize)
                .ToList();

            return Task.FromResult((orders, totalCount));
        }

        public Task<Order> UpdateAsync(Order order, CancellationToken cancellationToken = default)
        {
            lock (_lock)
            {
                if (!_orders.ContainsKey(order.Id))
                {
                    throw new InvalidOperationException($"Order with ID {order.Id} not found.");
                }

                _orders[order.Id] = order;
                return Task.FromResult(order);
            }
        }

        public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var removed = _orders.TryRemove(id, out _);
            return Task.FromResult(removed);
        }

        public Task<List<Order>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
        {
            var orders = _orders.Values
                .Where(o => o.CustomerId == customerId)
                .OrderByDescending(o => o.CreatedAt)
                .ToList();

            return Task.FromResult(orders);
        }

        public Task<bool> OrderNumberExistsAsync(string orderNumber, CancellationToken cancellationToken = default)
        {
            var exists = _orders.Values.Any(o => o.OrderNumber == orderNumber);
            return Task.FromResult(exists);
        }
    }
}
