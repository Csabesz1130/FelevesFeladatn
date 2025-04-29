using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Microsoft.EntityFrameworkCore;
using BRK0Y5_HSZF_2024252.Application.Interfaces;
using BRK0Y5_HSZF_2024252.Model;
using BRK0Y5_HSZF_2024252.Persistence.MsSql;

namespace BRK0Y5_HSZF_2024252.Application.Services
{
    /// <summary>
    /// Service that imports TaxiCar/Fare data from JSON or XML files.
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
            // Check file extension to determine format
            var extension = Path.GetExtension(filePath).ToLower();

            if (extension == ".json")
            {
                await ImportJsonDataAsync(filePath);
            }
            else if (extension == ".xml")
            {
                await ImportXmlDataAsync(filePath);
            }
            else
            {
                throw new Exception($"Unsupported file format: {extension}. Only .json and .xml are supported.");
            }
        }

        private async Task ImportJsonDataAsync(string filePath)
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
                        Driver = dto.Driver,
                        Model = "Unknown",  // Default value, not in JSON
                        TotalDistance = 0,  // Initialize distance fields
                        DistanceSinceLastMaintenance = 0
                    };

                    // Attach fares
                    newCar.Fares = allFares;
                    await _context.TaxiCars.AddAsync(newCar);
                }
            }

            // 4) Save changes
            await _context.SaveChangesAsync();
        }

        private async Task ImportXmlDataAsync(string filePath)
        {
            try
            {
                // Create serializer for CarSharingDto
                var serializer = new XmlSerializer(typeof(CarSharingDto));
                
                // Read the XML file
                using (var reader = new StreamReader(filePath))
                {
                    // Deserialize XML to CarSharingDto
                    var carSharing = (CarSharingDto)serializer.Deserialize(reader);
                    
                    // Import cars
                    if (carSharing.Cars != null && carSharing.Cars.Items != null)
                    {
                        foreach (var car in carSharing.Cars.Items)
                        {
                            var existingCar = await _context.TaxiCars
                                .FirstOrDefaultAsync(c => c.Id == car.Id || c.LicensePlate == car.LicensePlate);
                                
                            if (existingCar == null)
                            {
                                // If LicensePlate not specified, generate one
                                if (string.IsNullOrEmpty(car.LicensePlate))
                                {
                                    car.LicensePlate = $"CAR-{car.Id}";
                                }
                                
                                // If Driver not specified, generate one
                                if (string.IsNullOrEmpty(car.Driver))
                                {
                                    car.Driver = $"Driver-{car.Id}";
                                }
                                
                                await _context.TaxiCars.AddAsync(car);
                            }
                            else
                            {
                                // Update existing car
                                existingCar.Model = car.Model;
                                existingCar.TotalDistance = car.TotalDistance;
                                existingCar.DistanceSinceLastMaintenance = car.DistanceSinceLastMaintenance;
                                
                                // Only update these if specified
                                if (!string.IsNullOrEmpty(car.LicensePlate))
                                {
                                    existingCar.LicensePlate = car.LicensePlate;
                                }
                                
                                if (!string.IsNullOrEmpty(car.Driver))
                                {
                                    existingCar.Driver = car.Driver;
                                }
                                
                                _context.TaxiCars.Update(existingCar);
                            }
                        }
                    }
                    
                    // Import customers
                    if (carSharing.Customers != null && carSharing.Customers.Items != null)
                    {
                        foreach (var customer in carSharing.Customers.Items)
                        {
                            var existingCustomer = await _context.Customers
                                .FirstOrDefaultAsync(c => c.Id == customer.Id);
                                
                            if (existingCustomer == null)
                            {
                                await _context.Customers.AddAsync(customer);
                            }
                            else
                            {
                                existingCustomer.Name = customer.Name;
                                existingCustomer.Balance = customer.Balance;
                                _context.Customers.Update(existingCustomer);
                            }
                        }
                    }
                    
                    // Import trips/fares
                    if (carSharing.Trips != null && carSharing.Trips.Items != null)
                    {
                        foreach (var trip in carSharing.Trips.Items)
                        {
                            var existingTrip = await _context.Fares
                                .FirstOrDefaultAsync(t => t.Id == trip.Id);
                                
                            if (existingTrip == null)
                            {
                                await _context.Fares.AddAsync(trip);
                            }
                            else
                            {
                                existingTrip.CarId = trip.CarId;
                                existingTrip.CustomerId = trip.CustomerId;
                                existingTrip.Distance = trip.Distance;
                                existingTrip.PaidAmount = trip.PaidAmount;
                                _context.Fares.Update(existingTrip);
                            }
                        }
                    }
                    
                    // Save all changes
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error importing XML: {ex.Message}", ex);
            }
        }
    }
}