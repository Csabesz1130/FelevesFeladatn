// StatisticsService.cs
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BRK0Y5_HSZF_2024252.Application.Interfaces;
using BRK0Y5_HSZF_2024252.Model;
using BRK0Y5_HSZF_2024252.Persistence.MsSql;

namespace BRK0Y5_HSZF_2024252.Application.Services
{
    /// <summary>
    /// Generates statistics about taxi cars, fares, and car sharing data.
    /// Writes the results to "TaxiStatistics.txt" and "CarSharingStatistics.txt".
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
            // Run both stats generators
            await GenerateTaxiStatisticsAsync();
            await GenerateCarSharingStatisticsAsync();
        }

        private async Task GenerateTaxiStatisticsAsync()
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
                    $"Distance={longestFare.Distance} km, CarId={longestFare.CarId}");
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

        private async Task GenerateCarSharingStatisticsAsync()
        {
            // Load all cars with their Fares
            var cars = await _context.TaxiCars
                .Include(c => c.Fares)
                .ToListAsync();

            // Load all customers
            var customers = await _context.Customers
                .ToListAsync();

            // Build a text report
            var sb = new StringBuilder();
            sb.AppendLine("=== Car Sharing Statistics ===");
            sb.AppendLine($"Report Date: {DateTime.Now}");
            sb.AppendLine();

            // Most used car
            var mostUsedCar = cars.OrderByDescending(c => c.TotalDistance).FirstOrDefault();
            if (mostUsedCar != null)
            {
                sb.AppendLine($"Most Used Car: {mostUsedCar.Model} (ID: {mostUsedCar.Id}, License: {mostUsedCar.LicensePlate})");
                sb.AppendLine($"Total Distance: {mostUsedCar.TotalDistance:F2} km");
                sb.AppendLine();
            }

            // Average car distance
            double avgCarDistance = cars.Any() ? cars.Average(c => c.TotalDistance) : 0;
            sb.AppendLine($"Average Car Distance: {avgCarDistance:F2} km");
            sb.AppendLine();

            // Cars needing maintenance
            var carsNeedingMaintenance = cars.Where(c => c.NeedsMaintenance).ToList();
            sb.AppendLine($"Cars Needing Maintenance: {carsNeedingMaintenance.Count}");
            foreach (var car in carsNeedingMaintenance)
            {
                sb.AppendLine($"  {car.Model} (ID: {car.Id}, License: {car.LicensePlate}): {car.DistanceSinceLastMaintenance:F2} km since last maintenance");
            }
            sb.AppendLine();

            // Top paying customers
            var topCustomers = (from fare in _context.Fares
                              group fare by fare.CustomerId into g
                              select new { 
                                  CustomerId = g.Key, 
                                  TotalSpent = g.Sum(f => f.PaidAmount) 
                              })
                              .OrderByDescending(x => x.TotalSpent)
                              .Take(10)
                              .ToList();

            sb.AppendLine("Top Paying Customers:");
            int rank = 1;
            foreach (var customerSpending in topCustomers)
            {
                var customer = customers.FirstOrDefault(c => c.Id == customerSpending.CustomerId);
                if (customer != null)
                {
                    sb.AppendLine($"  {rank}. {customer.Name} (ID: {customer.Id}): {customerSpending.TotalSpent:F2}€");
                    rank++;
                }
            }
            sb.AppendLine();

            // Customers with insufficient funds
            var customersWithInsufficientFunds = customers.Where(c => c.Balance < 40).ToList();
            sb.AppendLine("Customers with Insufficient Funds (< 40€):");
            foreach (var customer in customersWithInsufficientFunds)
            {
                sb.AppendLine($"  {customer.Name} (ID: {customer.Id}): {customer.Balance:F2}€");
            }

            // Write to "CarSharingStatistics.txt"
            File.WriteAllText("CarSharingStatistics.txt", sb.ToString());

            // Also print the summary to console
            Console.WriteLine(sb.ToString());
            Console.WriteLine("Statistics saved to CarSharingStatistics.txt");
        }
    }
}