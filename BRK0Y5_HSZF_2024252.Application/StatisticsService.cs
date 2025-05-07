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
    public class StatisticsService : IStatisticsService
    {
        private readonly TaxiDbContext _context;

        public StatisticsService(TaxiDbContext context)
        {
            _context = context;
        }

        public async Task GenerateStatisticsAsync()
        {
            await GenerateTaxiStatisticsAsync();
            await GenerateCarSharingStatisticsAsync();
        }

        private async Task GenerateTaxiStatisticsAsync()
        {
            var cars = await _context.TaxiCars
                .Include(c => c.Fares)
                .ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("=== Taxi Statistics ===");
            sb.AppendLine($"Report Date: {DateTime.Now}");
            sb.AppendLine();

            sb.AppendLine("Short Trips (Distance < 10km) per Car:");
            foreach (var car in cars)
            {
                int shortTripCount = car.Fares.Count(f => f.Distance < 10);
                sb.AppendLine($"  {car.LicensePlate}: {shortTripCount} short trip(s)");
            }
            sb.AppendLine();

            sb.AppendLine("Average Distance per Car:");
            foreach (var car in cars)
            {
                double avgDistance = car.Fares.Any()
                    ? car.Fares.Average(f => f.Distance)
                    : 0.0;
                sb.AppendLine($"  {car.LicensePlate}: {avgDistance:F2} km");
            }
            sb.AppendLine();

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

            File.WriteAllText("TaxiStatistics.txt", sb.ToString());

            Console.WriteLine(sb.ToString());
            Console.WriteLine("Statistics saved to TaxiStatistics.txt");
        }

        private async Task GenerateCarSharingStatisticsAsync()
        {
            var cars = await _context.TaxiCars
                .Include(c => c.Fares)
                .ToListAsync();

            var customers = await _context.Customers
                .ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("=== Car Sharing Statistics ===");
            sb.AppendLine($"Report Date: {DateTime.Now}");
            sb.AppendLine();

            var mostUsedCar = cars.OrderByDescending(c => c.TotalDistance).FirstOrDefault();
            if (mostUsedCar != null)
            {
                sb.AppendLine($"Most Used Car: {mostUsedCar.Model} (ID: {mostUsedCar.Id}, License: {mostUsedCar.LicensePlate})");
                sb.AppendLine($"Total Distance: {mostUsedCar.TotalDistance:F2} km");
                sb.AppendLine();
            }

            double avgCarDistance = cars.Any() ? cars.Average(c => c.TotalDistance) : 0;
            sb.AppendLine($"Average Car Distance: {avgCarDistance:F2} km");
            sb.AppendLine();

            var carsNeedingMaintenance = cars.Where(c => c.NeedsMaintenance).ToList();
            sb.AppendLine($"Cars Needing Maintenance: {carsNeedingMaintenance.Count}");
            foreach (var car in carsNeedingMaintenance)
            {
                sb.AppendLine($"  {car.Model} (ID: {car.Id}, License: {car.LicensePlate}): {car.DistanceSinceLastMaintenance:F2} km since last maintenance");
            }
            sb.AppendLine();

            var topCustomers = _context.Fares
                .AsEnumerable()
                .GroupBy(fare => fare.CustomerId)
                .Select(g => new {
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

            var customersWithInsufficientFunds = customers.Where(c => c.Balance < 40).ToList();
            sb.AppendLine("Customers with Insufficient Funds (< 40€):");
            foreach (var customer in customersWithInsufficientFunds)
            {
                sb.AppendLine($"  {customer.Name} (ID: {customer.Id}): {customer.Balance:F2}€");
            }

            File.WriteAllText("CarSharingStatistics.txt", sb.ToString());

            Console.WriteLine(sb.ToString());
            Console.WriteLine("Statistics saved to CarSharingStatistics.txt");
        }
    }
}