using LogisticsTracker.Orders.DbContext;
using LogisticsTracker.Orders.Models;
using LogisticsTracker.Orders.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace LogisticsTracker.Orders.Repository
{
    public class PostgresOrderRepository : IOrderRepository
    {
        private readonly OrdersDbContext _context;
        private readonly ILogger<PostgresOrderRepository> _logger;

        public PostgresOrderRepository(
            OrdersDbContext context,
            ILogger<PostgresOrderRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Order> CreateAsync(Order order, CancellationToken cancellationToken = default)
        {
            _context.Orders.Add(order);
            await _context.SaveChangesAsync(cancellationToken);
            return order;
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var order = await GetByIdAsync(id, cancellationToken);
            if (order == null)
            {
                return false;
            }

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<(List<Order> Orders, int TotalCount)> GetAllAsync(OrderQueryParameters queryParams, CancellationToken cancellationToken = default)
        {
            var query = _context.Orders.AsQueryable();
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

            var totalCount = await query.CountAsync(cancellationToken);

            var orders = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((queryParams.PageNumber - 1) * queryParams.PageSize)
                .Take(queryParams.PageSize)
                .ToListAsync(cancellationToken);
            return (orders, totalCount);
        }

        public async Task<List<Order>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
        {
            return await _context.Orders
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken);
        }

        public async Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.Orders
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
        }

        public async Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default)
        {
            return await _context.Orders
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber, cancellationToken);
        }

        public async Task<bool> OrderNumberExistsAsync(string orderNumber, CancellationToken cancellationToken = default)
        {
            return await _context.Orders
            .AnyAsync(o => o.OrderNumber == orderNumber, cancellationToken);
        }

        public async Task<Order> UpdateAsync(Order order, CancellationToken cancellationToken = default)
        {
            _context.Orders.Update(order);
            await _context.SaveChangesAsync(cancellationToken);
            return order;
        }
    }
}
