using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Microsoft.EntityFrameworkCore;
using BRK0Y5_HSZF_2024252.Model;
using BRK0Y5_HSZF_2024252.Application.Interfaces;
using BRK0Y5_HSZF_2024252.Persistence.MsSql;

namespace BRK0Y5_HSZF_2024252.Application.Services
{
    public class CarManagementService : ICarManagementService
    {
        private readonly TaxiDbContext _context;
        private readonly Random _random = new Random();

        public delegate void TripEventHandler(object sender, TripEventArgs e);
        public delegate void MaintenanceEventHandler(object sender, MaintenanceEventArgs e);
        public delegate void InsufficientFundsEventHandler(object sender, InsufficientFundsEventArgs e);

        public event TripEventHandler TripStarted;
        public event TripEventHandler TripFinished;
        public event MaintenanceEventHandler MaintenanceRequested;
        public event InsufficientFundsEventHandler InsufficientFunds;

        public CarManagementService(TaxiDbContext context)
        {
            _context = context;
        }

        #region Original Methods

        public async Task<List<TaxiCar>> GetCarsAsync()
        {
            return await _context.TaxiCars.ToListAsync();
        }

        public async Task<TaxiCar> GetCarByLicensePlateAsync(string licensePlate)
        {
            return await _context.TaxiCars
                .FirstOrDefaultAsync(tc => tc.LicensePlate == licensePlate);
        }

        public async Task AddCarAsync(TaxiCar car)
        {
            _context.TaxiCars.Add(car);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateCarAsync(TaxiCar car)
        {
            _context.TaxiCars.Update(car);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteCarAsync(string licensePlate)
        {
            var car = await GetCarByLicensePlateAsync(licensePlate);
            if (car != null)
            {
                _context.TaxiCars.Remove(car);
                await _context.SaveChangesAsync();
            }
        }

        public async Task AddFareAsync(string licensePlate, Fare fare, Action<string> notification = null)
        {
            var car = await GetCarByLicensePlateAsync(licensePlate);
            if (car == null)
                throw new Exception($"Car not found: {licensePlate}");

            if (fare.PaidAmount > 9999)
            {
                notification?.Invoke($"Warning: a very expensive fare was added!");
            }

            car.Fares.Add(fare);
            await _context.SaveChangesAsync();

            car.TotalDistance += fare.Distance;
            car.DistanceSinceLastMaintenance += fare.Distance;
            await _context.SaveChangesAsync();

            await CheckForMaintenanceAsync(car);
        }

        public async Task<List<TaxiCar>> SearchCarsAsync(string licensePlate = null, string driver = null)
        {
            var query = _context.TaxiCars.AsQueryable();

            if (!string.IsNullOrWhiteSpace(licensePlate))
            {
                var lower = licensePlate.ToLower();
                query = query.Where(x => x.LicensePlate.ToLower().Contains(lower));
            }

            if (!string.IsNullOrWhiteSpace(driver))
            {
                var lowerDriver = driver.ToLower();
                query = query.Where(x => x.Driver.ToLower().Contains(lowerDriver));
            }

            return await query.ToListAsync();
        }

        #endregion

        #region Car Methods

        public async Task<TaxiCar> GetCarByIdAsync(int id)
        {
            return await _context.TaxiCars.FindAsync(id);
        }

        public async Task PerformMaintenanceAsync(string licensePlate)
        {
            var car = await GetCarByLicensePlateAsync(licensePlate);
            if (car != null)
            {
                car.DistanceSinceLastMaintenance = 0;
                car.LastServiceDate = DateTime.UtcNow;

                _context.TaxiCars.Update(car);
                await _context.SaveChangesAsync();

                MaintenanceRequested?.Invoke(this, new MaintenanceEventArgs
                {
                    CarId = car.Id,
                    CarModel = car.Model,
                    LicensePlate = car.LicensePlate,
                    TotalDistance = car.TotalDistance
                });
            }
        }

        private async Task CheckForMaintenanceAsync(TaxiCar car)
        {
            bool needsMaintenance = false;

            if (car.DistanceSinceLastMaintenance >= 200)
            {
                needsMaintenance = true;
            }

            else if (_random.Next(100) < 20)
            {
                needsMaintenance = true;
            }

            if (needsMaintenance)
            {
                await PerformMaintenanceAsync(car.LicensePlate);
            }
        }

        #endregion

        #region Customer Methods

        public async Task<List<Customer>> GetCustomersAsync()
        {
            return await _context.Customers.ToListAsync();
        }

        public async Task<Customer> GetCustomerByIdAsync(int id)
        {
            return await _context.Customers.FindAsync(id);
        }

        public async Task<Customer> GetCustomerByNameAsync(string name)
        {
            return await _context.Customers
                .FirstOrDefaultAsync(c => c.Name.ToLower() == name.ToLower());
        }

        public async Task AddCustomerAsync(Customer customer)
        {
            await _context.Customers.AddAsync(customer);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateCustomerAsync(Customer customer)
        {
            _context.Customers.Update(customer);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteCustomerAsync(int id)
        {
            var customer = await GetCustomerByIdAsync(id);
            if (customer != null)
            {
                _context.Customers.Remove(customer);
                await _context.SaveChangesAsync();
            }
        }

        #endregion

        #region Trip Methods

        public async Task<bool> StartTripAsync(string licensePlate, int customerId, double estimatedDistance)
        {
            var car = await GetCarByLicensePlateAsync(licensePlate);
            var customer = await GetCustomerByIdAsync(customerId);

            if (car == null || customer == null)
                return false;

            if (customer.Balance < 40m)
            {
                InsufficientFunds?.Invoke(this, new InsufficientFundsEventArgs
                {
                    CustomerId = customer.Id,
                    CustomerName = customer.Name,
                    CurrentBalance = customer.Balance,
                    MinimumBalance = 40m
                });

                return false;
            }

            decimal estimatedCost = Fare.CalculateCost(estimatedDistance);

            TripStarted?.Invoke(this, new TripEventArgs
            {
                CarId = car.Id,
                LicensePlate = car.LicensePlate,
                CarModel = car.Model,
                CustomerId = customer.Id,
                CustomerName = customer.Name,
                Distance = estimatedDistance,
                Cost = estimatedCost
            });

            return true;
        }

        public async Task FinishTripAsync(string licensePlate, int customerId, double actualDistance)
        {
            var car = await GetCarByLicensePlateAsync(licensePlate);
            var customer = await GetCustomerByIdAsync(customerId);

            if (car == null || customer == null)
                return;

            decimal actualCost = Fare.CalculateCost(actualDistance);

            var fare = new Fare
            {
                CarId = car.Id,
                CustomerId = customerId,
                Distance = actualDistance,
                PaidAmount = actualCost,
                From = "Trip Start",
                To = "Trip End",
                FareStartDate = DateTime.UtcNow
            };

            _context.Fares.Add(fare);

            customer.Balance -= actualCost;
            _context.Customers.Update(customer);

            car.TotalDistance += actualDistance;
            car.DistanceSinceLastMaintenance += actualDistance;
            _context.TaxiCars.Update(car);

            await _context.SaveChangesAsync();

            TripFinished?.Invoke(this, new TripEventArgs
            {
                CarId = car.Id,
                LicensePlate = car.LicensePlate,
                CarModel = car.Model,
                CustomerId = customer.Id,
                CustomerName = customer.Name,
                Distance = actualDistance,
                Cost = actualCost
            });

            await CheckForMaintenanceAsync(car);
        }

        #endregion

        #region Queries

        public async Task<TaxiCar> GetMostUsedCarAsync()
        {
            return await _context.TaxiCars
                .OrderByDescending(c => c.TotalDistance)
                .FirstOrDefaultAsync();
        }

        public async Task<List<Customer>> GetTopPayingCustomersAsync(int count = 10)
        {
            // Use AsEnumerable to move the grouping and aggregation to memory
            var customerSpending = _context.Fares
                .AsEnumerable()
                .GroupBy(t => t.CustomerId)
                .Select(g => new
                {
                    CustomerId = g.Key,
                    TotalSpent = g.Sum(t => t.PaidAmount)
                })
                .OrderByDescending(x => x.TotalSpent)
                .Take(count)
                .ToList();

            var topCustomers = new List<Customer>();
            foreach (var spending in customerSpending)
            {
                var customer = await _context.Customers.FindAsync(spending.CustomerId);
                if (customer != null)
                {
                    topCustomers.Add(customer);
                }
            }

            return topCustomers;
        }

        public async Task<double> GetAverageCarDistanceAsync()
        {
            if (!await _context.TaxiCars.AnyAsync())
                return 0;

            return await _context.TaxiCars.AverageAsync(c => c.TotalDistance);
        }

        #endregion

        #region Export Methods

        public async Task ExportCarsToCSVAsync(string filePath = "cars.csv")
        {
            var cars = await GetCarsAsync();
            var csv = new StringBuilder();

            csv.AppendLine("Id,LicensePlate,Model,Driver,TotalDistance,DistanceSinceLastMaintenance");

            foreach (var car in cars)
            {
                csv.AppendLine($"{car.Id},{car.LicensePlate},{car.Model},{car.Driver},{car.TotalDistance},{car.DistanceSinceLastMaintenance}");
            }

            await File.WriteAllTextAsync(filePath, csv.ToString());
        }

        public async Task ExportCustomersToCSVAsync(string filePath = "customers.csv")
        {
            var customers = await GetCustomersAsync();
            var csv = new StringBuilder();

            csv.AppendLine("Id,Name,Balance");

            foreach (var customer in customers)
            {
                csv.AppendLine($"{customer.Id},{customer.Name},{customer.Balance}");
            }

            await File.WriteAllTextAsync(filePath, csv.ToString());
        }

        public async Task ExportTripsToCSVAsync(string filePath = "trips.csv")
        {
            var trips = await _context.Fares.ToListAsync();
            var csv = new StringBuilder();

            csv.AppendLine("Id,CarId,CustomerId,Distance,Cost,From,To,FareStartDate");

            foreach (var trip in trips)
            {
                csv.AppendLine($"{trip.Id},{trip.CarId},{trip.CustomerId},{trip.Distance},{trip.PaidAmount},\"{trip.From}\",\"{trip.To}\",{trip.FareStartDate}");
            }

            await File.WriteAllTextAsync(filePath, csv.ToString());
        }

        #endregion
    }

    #region Event Argument Classes

    public class TripEventArgs : EventArgs
    {
        public int CarId { get; set; }
        public string LicensePlate { get; set; }
        public string CarModel { get; set; }
        public int CustomerId { get; set; }
        public string CustomerName { get; set; }
        public double Distance { get; set; }
        public decimal Cost { get; set; }
    }

    public class MaintenanceEventArgs : EventArgs
    {
        public int CarId { get; set; }
        public string LicensePlate { get; set; }
        public string CarModel { get; set; }
        public double TotalDistance { get; set; }
    }

    public class InsufficientFundsEventArgs : EventArgs
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; }
        public decimal CurrentBalance { get; set; }
        public decimal MinimumBalance { get; set; }
    }

    #endregion
}