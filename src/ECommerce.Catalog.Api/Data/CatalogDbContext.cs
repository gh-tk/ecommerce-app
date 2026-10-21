using ECommerce.Catalog.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Catalog.Api.Data;

public class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Name).HasMaxLength(100).IsRequired();
            entity.HasIndex(c => c.Name).IsUnique();
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Products_Price_NonNegative", "\"Price\" >= 0");
                t.HasCheckConstraint("CK_Products_AvailableStock_NonNegative", "\"AvailableStock\" >= 0");
            });

            entity.HasKey(p => p.Id);
            entity.Property(p => p.Sku).HasMaxLength(40).IsRequired();
            entity.HasIndex(p => p.Sku).IsUnique();
            entity.Property(p => p.Name).HasMaxLength(200).IsRequired();
            entity.Property(p => p.Description).HasMaxLength(2000);
            entity.Property(p => p.Price).HasPrecision(18, 2);

            // No navigation properties: entities stay directly serializable.
            entity.HasOne<Category>()
                .WithMany()
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(p => p.CategoryId);
        });

        // Synthetic seed data so the service is usable immediately.
        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Peripherals" },
            new Category { Id = 2, Name = "Displays" },
            new Category { Id = 3, Name = "Accessories" });

        modelBuilder.Entity<Product>().HasData(
            new Product { Id = 1, Sku = "KB-MECH-001", Name = "Mechanical Keyboard", Description = "Hot-swappable RGB keyboard", Price = 119.99m, AvailableStock = 50, CategoryId = 1 },
            new Product { Id = 2, Sku = "MS-WL-001", Name = "Wireless Mouse", Description = "Ergonomic 8k DPI mouse", Price = 49.50m, AvailableStock = 120, CategoryId = 1 },
            new Product { Id = 3, Sku = "MON-4K-27", Name = "4K Monitor", Description = "27-inch IPS display", Price = 329.00m, AvailableStock = 25, CategoryId = 2 },
            new Product { Id = 4, Sku = "HUB-USBC-7", Name = "USB-C Hub", Description = "7-in-1 docking hub", Price = 39.99m, AvailableStock = 200, CategoryId = 3 });
    }
}
