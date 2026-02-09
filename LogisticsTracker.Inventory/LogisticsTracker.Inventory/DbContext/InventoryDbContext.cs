using LogisticsTracker.Inventory.Models;
using Microsoft.EntityFrameworkCore;

namespace LogisticsTracker.Inventory.DbContext
{
    public class InventoryDbContext : Microsoft.EntityFrameworkCore.DbContext
    {
        public InventoryDbContext(DbContextOptions<InventoryDbContext> options)
        : base(options)
        {
        }

        public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
        public DbSet<InventoryReservation> Reservations => Set<InventoryReservation>();
        public DbSet<StockMovement> StockMovements => Set<StockMovement>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<InventoryItem>(entity =>
            {
                entity.ToTable("inventory_items");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ProductId).IsRequired();
                entity.Property(e => e.StockKeepingUnit).IsRequired();
                entity.Property(e => e.Name).IsRequired();
                entity.Property(e => e.Description);
                entity.Property(e => e.WarehouseLocation);
                entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
                entity.HasIndex(e => e.StockKeepingUnit).IsUnique();
                entity.HasIndex(e => e.ProductId).IsUnique();
            });

            modelBuilder.Entity<InventoryReservation>(entity =>
            {
                entity.ToTable("inventory_reservations");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ProductId).IsRequired();
                entity.Property(e => e.StockKeepingUnit).IsRequired();
                entity.Property(e => e.OrderId).IsRequired();
                entity.HasIndex(e => e.ProductId);
                entity.HasIndex(e => e.OrderId);
                entity.HasIndex(e => e.ReleasedAt);
            });

            modelBuilder.Entity<StockMovement>(entity =>
            {
                entity.ToTable("stock_movements");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ProductId).IsRequired();
                entity.Property(e => e.StockKeepingUnit).IsRequired();
                entity.Property(e => e.Reason);
                entity.HasIndex(e => e.ProductId);
                entity.HasIndex(e => e.MovementDate);
            });
        }

    }
}
