using LogisticsTracker.Orders.Models;
using Microsoft.EntityFrameworkCore;

namespace LogisticsTracker.Orders.DbContext
{
    public class OrdersDbContext : Microsoft.EntityFrameworkCore.DbContext
    {
        public OrdersDbContext(DbContextOptions<OrdersDbContext> options)
        : base(options)
        {
        }

        public DbSet<Order> Orders => Set<Order>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Order>(entity =>
            {
                entity.ToTable("orders");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.OrderNumber).IsRequired();
                entity.Property(e => e.CustomerId).IsRequired();
                entity.Property(e => e.CustomerName).IsRequired();
                entity.Property(e => e.CustomerEmail).IsRequired();
                entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
                entity.Property(e => e.Notes);
                entity.OwnsOne(e => e.ShippingAddress, address =>
                {
                    address.Property(a => a.Street).HasMaxLength(200);
                    address.Property(a => a.City).HasMaxLength(100);
                    address.Property(a => a.State).HasMaxLength(50);
                    address.Property(a => a.PostalCode).HasMaxLength(20);
                    address.Property(a => a.Country).HasMaxLength(100);
                });
                entity.OwnsMany(e => e.Items, items =>
                {
                    items.ToTable("order_items");
                    items.WithOwner().HasForeignKey("OrderId");
                    items.Property<int>("Id");
                    items.HasKey("Id");
                    items.Property(i => i.ProductId).IsRequired();
                    items.Property(i => i.ProductName).HasMaxLength(200);
                    items.Property(i => i.StockKeepingUnit).HasMaxLength(20);
                    items.Property(i => i.UnitPrice).HasPrecision(18, 2);
                });
                entity.Property(e => e.ReservationIds).HasColumnType("jsonb");
                entity.HasIndex(e => e.OrderNumber).IsUnique();
                entity.HasIndex(e => e.CustomerId);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.CreatedAt);
            });
        }
    }
}
