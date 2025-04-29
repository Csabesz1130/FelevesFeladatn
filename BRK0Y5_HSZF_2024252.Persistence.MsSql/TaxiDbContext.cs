using Microsoft.EntityFrameworkCore;
using BRK0Y5_HSZF_2024252.Model;

namespace BRK0Y5_HSZF_2024252.Persistence.MsSql
{
    public class TaxiDbContext : DbContext
    {
        public DbSet<TaxiCar> TaxiCars { get; set; }
        public DbSet<Fare> Fares { get; set; }
        public DbSet<Customer> Customers { get; set; }

        public TaxiDbContext(DbContextOptions<TaxiDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // TaxiCar entity configuration
            modelBuilder.Entity<TaxiCar>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.LicensePlate).IsRequired();
                entity.Property(e => e.Driver).IsRequired();
                entity.Property(e => e.Model).IsRequired();
                entity.Property(e => e.TotalDistance).IsRequired();
                entity.Property(e => e.DistanceSinceLastMaintenance).IsRequired();

                // One-to-Many: Car -> Fares
                entity.HasMany(e => e.Fares)
                      .WithOne(e => e.Car)
                      .HasForeignKey(e => e.CarId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Customer entity configuration
            modelBuilder.Entity<Customer>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired();
                entity.Property(e => e.Balance).IsRequired();
                
                // One-to-Many: Customer -> Fares
                entity.HasMany(e => e.Fares)
                      .WithOne(e => e.Customer)
                      .HasForeignKey(e => e.CustomerId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Fare/Trip entity configuration
            modelBuilder.Entity<Fare>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Distance).IsRequired();
                entity.Property(e => e.PaidAmount).IsRequired();
                entity.Property(e => e.From).IsRequired(false);
                entity.Property(e => e.To).IsRequired(false);
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}