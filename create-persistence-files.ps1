# create-persistence-files.ps1
# =============================
# This script creates the "BRK0Y5_HSZF_2024252.Persistence.MsSql" folder (if needed)
# and adds TaxiDbContext.cs and TaxiDbContextFactory.cs with sample EF Core content.

Write-Host "Creating persistence folder if it does not exist..."
New-Item -ItemType Directory -Force -Path ".\BRK0Y5_HSZF_2024252.Persistence.MsSql" | Out-Null

Write-Host "Generating TaxiDbContext.cs..."
@"
using Microsoft.EntityFrameworkCore;
using BRK0Y5_HSZF_2024252.Model;

namespace BRK0Y5_HSZF_2024252.Persistence.MsSql
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
"@ | Set-Content .\BRK0Y5_HSZF_2024252.Persistence.MsSql\TaxiDbContext.cs -Encoding UTF8

Write-Host "Generating TaxiDbContextFactory.cs..."
@"
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BRK0Y5_HSZF_2024252.Persistence.MsSql
{
    // Used for applying EF Core migrations at design time.
    public class TaxiDbContextFactory : IDesignTimeDbContextFactory<TaxiDbContext>
    {
        public TaxiDbContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<TaxiDbContext>();
            builder.UseSqlite(""Data Source=TaxiDatabase.db"");

            return new TaxiDbContext(builder.Options);
        }
    }
}
"@ | Set-Content .\BRK0Y5_HSZF_2024252.Persistence.MsSql\TaxiDbContextFactory.cs -Encoding UTF8

Write-Host "Persistence files have been created!"
