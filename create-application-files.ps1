# create-application-files.ps1
# ============================
# Creates the "BRK0Y5_HSZF_2024252.Application" folder if needed
# and writes an interface + service with sample logic.

Write-Host "Creating application folder if it does not exist..."
New-Item -ItemType Directory -Force -Path ".\BRK0Y5_HSZF_2024252.Application" | Out-Null

Write-Host "Generating ICarManagementService.cs..."
@"
using BRK0Y5_HSZF_2024252.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BRK0Y5_HSZF_2024252.Application.Interfaces
{
    public interface ICarManagementService
    {
        Task<List<TaxiCar>> GetCarsAsync();
        Task<TaxiCar> GetCarByLicensePlateAsync(string licensePlate);
        Task AddCarAsync(TaxiCar car);
        Task UpdateCarAsync(TaxiCar car);
        Task DeleteCarAsync(string licensePlate);

        // Example: Add a new fare + optional callback message
        Task AddFareAsync(string licensePlate, Fare fare, Action<string> notification = null);

        // Example: Search by partial license plate, partial driver
        Task<List<TaxiCar>> SearchCarsAsync(string licensePlate = null, string driver = null);
    }
}
"@ | Set-Content .\BRK0Y5_HSZF_2024252.Application\ICarManagementService.cs -Encoding UTF8

Write-Host "Generating CarManagementService.cs..."
@"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BRK0Y5_HSZF_2024252.Model;
using BRK0Y5_HSZF_2024252.Application.Interfaces;
using BRK0Y5_HSZF_2024252.Persistence.MsSql;

namespace BRK0Y5_HSZF_2024252.Application.Services
{
    public class CarManagementService : ICarManagementService
    {
        private readonly TaxiDbContext _context;

        public CarManagementService(TaxiDbContext context)
        {
            _context = context;
        }

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
                throw new Exception($""Car not found: {licensePlate}"");

            // Example: if PaidAmount > some threshold, notify
            if (fare.PaidAmount > 9999)
            {
                notification?.Invoke($""Warning: a very expensive fare was added!"");
            }

            car.Fares.Add(fare);
            await _context.SaveChangesAsync();
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
    }
}
"@ | Set-Content .\BRK0Y5_HSZF_2024252.Application\CarManagementService.cs -Encoding UTF8

Write-Host "Application files have been created!"
