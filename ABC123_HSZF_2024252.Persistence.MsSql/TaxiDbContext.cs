using Microsoft.EntityFrameworkCore;
using ABC123_HSZF_2024252.Model;

namespace ABC123_HSZF_2024252.Persistence.MsSql
{
    public class TaxiDbContext : DbContext
    {
        public DbSet<TaxiCar> TaxiCars { get; set; }
        public DbSet<Fare> Fares { get; set; }

        public TaxiDbContext(DbContextOptions<TaxiDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Example config for TaxiCar
            modelBuilder.Entity<TaxiCar>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.LicensePlate).IsRequired();
                entity.Property(e => e.Driver).IsRequired();

                // One-to-Many: Car -> Fares
                entity.HasMany(e => e.Fares)
                      .WithOne(e => e.Car)
                      .HasForeignKey(e => e.TaxiCarId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Example config for Fare
            modelBuilder.Entity<Fare>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.From).IsRequired();
                entity.Property(e => e.To).IsRequired();
                entity.Property(e => e.Distance).IsRequired();
                entity.Property(e => e.PaidAmount).IsRequired();
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}
