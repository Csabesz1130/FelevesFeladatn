// StatisticsService.cs
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ABC123_HSZF_2024252.Application.Interfaces;
using ABC123_HSZF_2024252.Model;
using ABC123_HSZF_2024252.Persistence.MsSql;

namespace ABC123_HSZF_2024252.Application.Services
{
    /// <summary>
    /// Generates simple statistics about taxis/fare data:
    ///  - Short trips, average distance,
    ///  - The longest trip, etc.
    /// Writes the results to "TaxiStatistics.txt".
    /// </summary>
    public class StatisticsService : IStatisticsService
    {
        private readonly TaxiDbContext _context;

        public StatisticsService(TaxiDbContext context)
        {
            _context = context;
        }

        public async Task GenerateStatisticsAsync()
        {
            // Load all cars with their Fares
            var cars = await _context.TaxiCars
                .Include(c => c.Fares)
                .ToListAsync();

            // Build a text report
            var sb = new StringBuilder();
            sb.AppendLine("=== Taxi Statistics ===");
            sb.AppendLine($"Report Date: {DateTime.Now}");
            sb.AppendLine();

            // 1) Count of short trips (< 10km) per car
            sb.AppendLine("Short Trips (Distance < 10km) per Car:");
            foreach (var car in cars)
            {
                int shortTripCount = car.Fares.Count(f => f.Distance < 10);
                sb.AppendLine($"  {car.LicensePlate}: {shortTripCount} short trip(s)");
            }
            sb.AppendLine();

            // 2) Average distance per car
            sb.AppendLine("Average Distance per Car:");
            foreach (var car in cars)
            {
                double avgDistance = car.Fares.Any()
                    ? car.Fares.Average(f => f.Distance)
                    : 0.0;
                sb.AppendLine($"  {car.LicensePlate}: {avgDistance:F2} km");
            }
            sb.AppendLine();

            // 3) Find the single longest trip among all cars
            var longestFare = cars.SelectMany(c => c.Fares)
                                  .OrderByDescending(f => f.Distance)
                                  .FirstOrDefault();

            if (longestFare != null)
            {
                sb.AppendLine($"Longest Fare: {longestFare.From} -> {longestFare.To}, " +
                    $"Distance={longestFare.Distance} km, CarId={longestFare.TaxiCarId}");
            }
            else
            {
                sb.AppendLine("No fares found to determine the longest trip.");
            }

            // Write to "TaxiStatistics.txt"
            File.WriteAllText("TaxiStatistics.txt", sb.ToString());

            // Also print the summary to console
            Console.WriteLine(sb.ToString());
            Console.WriteLine("Statistics saved to TaxiStatistics.txt");
        }
    }
}
