using LogisticsTracker.Inventory.DbContext;
using LogisticsTracker.Inventory.Models;
using Microsoft.EntityFrameworkCore;

namespace LogisticsTracker.Inventory.Repository
{
    public class PostgresInventoryRepository : IInventoryRepository
    {
        private readonly InventoryDbContext _context;
        private readonly ILogger<PostgresInventoryRepository> _logger;

        public PostgresInventoryRepository(
            InventoryDbContext context,
            ILogger<PostgresInventoryRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<InventoryItem> CreateAsync(InventoryItem item, CancellationToken cancellationToken = default)
        {
            _context.InventoryItems.Add(item);
            await _context.SaveChangesAsync(cancellationToken);
            return item;
        }

        public async Task<InventoryReservation> CreateReservationAsync(InventoryReservation reservation, CancellationToken cancellationToken = default)
        {
            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync(cancellationToken);
            return reservation;
        }

        public async Task<bool> DeleteAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            var item = await GetByProductIdAsync(productId, cancellationToken);
            if (item == null)
            {
                return false;
            }

            _context.InventoryItems.Remove(item);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<List<InventoryReservation>> GetActiveReservationsAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            return await _context.Reservations
            .Where(r => r.ProductId == productId && r.ReleasedAt == null)
            .OrderBy(r => r.ReservedAt)
            .ToListAsync(cancellationToken);
        }

        public async Task<List<InventoryItem>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.InventoryItems
            .OrderBy(i => i.Name)
            .ToListAsync(cancellationToken);
        }

        public async Task<InventoryItem?> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
        {
            return await _context.InventoryItems
            .FirstOrDefaultAsync(i => i.ProductId == productId, cancellationToken);
        }

        public async Task<InventoryItem?> GetByStockKeepingUnitAsync(string stockKeepingUnit, CancellationToken cancellationToken = default)
        {
            return await _context.InventoryItems
            .FirstOrDefaultAsync(i => i.StockKeepingUnit == stockKeepingUnit, cancellationToken);
        }

        public async Task<List<InventoryItem>> GetLowStockItemsAsync(CancellationToken cancellationToken = default)
        {
            return await _context.InventoryItems
            .Where(i => (i.QuantityAvailable + i.QuantityReserved) <= i.ReorderPoint)
            .OrderBy(i => i.QuantityAvailable + i.QuantityReserved)
            .ToListAsync(cancellationToken);
        }

        public async Task<InventoryReservation?> GetReservationAsync(Guid reservationId, CancellationToken cancellationToken = default)
        {
            return await _context.Reservations
            .FirstOrDefaultAsync(r => r.Id == reservationId, cancellationToken);
        }

        public async Task<bool> StockKeepingUnitExistsAsync(string stockKeepingUnit, CancellationToken cancellationToken = default)
        {
            return await _context.InventoryItems
            .AnyAsync(i => i.StockKeepingUnit == stockKeepingUnit, cancellationToken);
        }

        public async Task<InventoryItem> UpdateAsync(InventoryItem item, CancellationToken cancellationToken = default)
        {
            _context.InventoryItems.Update(item);
            await _context.SaveChangesAsync(cancellationToken);

            return item;
        }

        public async Task<InventoryReservation> UpdateReservationAsync(InventoryReservation reservation, CancellationToken cancellationToken = default)
        {
            _context.Reservations.Update(reservation);
            await _context.SaveChangesAsync(cancellationToken);

            return reservation;
        }
    }
}
