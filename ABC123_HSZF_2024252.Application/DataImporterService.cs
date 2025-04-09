using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ABC123_HSZF_2024252.Application.Interfaces;
using ABC123_HSZF_2024252.Model;
using ABC123_HSZF_2024252.Persistence.MsSql;

namespace ABC123_HSZF_2024252.Application.Services
{
    /// <summary>
    /// Sample service that imports TaxiCar/Fare data from a JSON file.
    /// Adjust as needed for XML or other formats.
    /// </summary>
    public class DataImporterService : IDataImporterService
    {
        private readonly TaxiDbContext _context;

        public DataImporterService(TaxiDbContext context)
        {
            _context = context;
        }

        public async Task ImportDataAsync(string filePath)
        {
            // 1) Load the JSON as text
            var jsonData = await File.ReadAllTextAsync(filePath);

            // 2) Deserialize it into a "wrapper" object or a list
            //    Example: we expect an array of TaxiCarDto or some root object
            var carDtos = JsonSerializer.Deserialize<TaxiCarDto[]>(jsonData);

            if (carDtos == null || !carDtos.Any())
            {
                throw new Exception("No data found or invalid JSON format.");
            }

            // 3) Process each DTO, converting it to domain entities
            foreach (var dto in carDtos)
            {
                // Convert the Dto to a domain entity
                var existingCar = await _context.TaxiCars
                    .Include(tc => tc.Fares)
                    .FirstOrDefaultAsync(tc => tc.LicensePlate == dto.LicensePlate);

                // Gather all fare-like entries (Fares + Services)
                var allFares = dto.GetAllFares()
                                  .Select(fd => new Fare
                                  {
                                      From = fd.From,
                                      To = fd.To,
                                      Distance = fd.Distance,
                                      PaidAmount = fd.PaidAmount,
                                      FareStartDate = fd.FareStartDate
                                      // Possibly set more fields if needed
                                  })
                                  .ToList();

                if (existingCar != null)
                {
                    // Add only new fares
                    foreach (var fare in allFares)
                    {
                        bool alreadyExists = existingCar.Fares.Any(f => 
                            f.FareStartDate == fare.FareStartDate && 
                            f.From == fare.From &&
                            f.To == fare.To);

                        if (!alreadyExists)
                        {
                            existingCar.Fares.Add(fare);
                        }
                    }
                }
                else
                {
                    // Create a new TaxiCar
                    var newCar = new TaxiCar
                    {
                        LicensePlate = dto.LicensePlate,
                        Driver = dto.Driver
                        // Optionally handle other fields (VehicleType, etc.)
                    };

                    // Attach fares
                    newCar.Fares = allFares;
                    await _context.TaxiCars.AddAsync(newCar);
                }
            }

            // 4) Save changes
            await _context.SaveChangesAsync();
        }
    }
}
