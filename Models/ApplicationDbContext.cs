using Microsoft.EntityFrameworkCore;

namespace ABC_Retailer.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<Cart> Cart { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // For in-memory database, we don't need SQL-specific configurations
            // But we'll keep the basic configurations

            // User configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.RowKey);
                entity.Property(e => e.RowKey).HasMaxLength(50);
                entity.Property(e => e.Username).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Email).HasMaxLength(255).IsRequired();
                entity.Property(e => e.PasswordHash).HasMaxLength(256).IsRequired();
                entity.Property(e => e.FirstName).HasMaxLength(100).IsRequired();
                entity.Property(e => e.LastName).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Role).HasMaxLength(20).HasDefaultValue("Customer");
            });

            // Customer configuration
            modelBuilder.Entity<Customer>(entity =>
            {
                entity.HasKey(e => e.RowKey);
                entity.Property(e => e.RowKey).HasMaxLength(50);
                entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Surname).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Username).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Email).HasMaxLength(255).IsRequired();
                entity.Property(e => e.ShippingAddress).HasMaxLength(500);
            });

            // Product configuration
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(e => e.RowKey);
                entity.Property(e => e.RowKey).HasMaxLength(50);
                entity.Property(e => e.ProductName).HasMaxLength(255).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
            });

            // Order configuration
            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasKey(e => e.RowKey);
                entity.Property(e => e.RowKey).HasMaxLength(50);
                entity.Property(e => e.CustomerId).HasMaxLength(50).IsRequired();
                entity.Property(e => e.CustomerName).HasMaxLength(255).IsRequired();
                entity.Property(e => e.ProductId).HasMaxLength(50).IsRequired();
                entity.Property(e => e.ProductName).HasMaxLength(255).IsRequired();
                entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");
                entity.Property(e => e.TotalPrice).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Status).HasMaxLength(50).HasDefaultValue("Pending");
            });

            // Cart configuration
            modelBuilder.Entity<Cart>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.CustomerUsername).HasMaxLength(100).IsRequired();
                entity.Property(e => e.ProductId).HasMaxLength(50).IsRequired();
                entity.Property(e => e.ProductName).HasMaxLength(255).IsRequired();
                entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");
            });
        }
    }
}