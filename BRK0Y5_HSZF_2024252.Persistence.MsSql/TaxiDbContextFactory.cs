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
            builder.UseSqlite("Data Source=TaxiDatabase.db");

            return new TaxiDbContext(builder.Options);
        }
    }
}
