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
    
    
    
    public class DataImporterService : IDataImporterService
    {
        private readonly TaxiDbContext _context;

        public DataImporterService(TaxiDbContext context)
        {
            _context = context;
        }

        public async Task ImportDataAsync(string filePath)
        {
            
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
            
            var jsonData = await File.ReadAllTextAsync(filePath);

            
            
            var carDtos = JsonSerializer.Deserialize<TaxiCarDto[]>(jsonData);

            if (carDtos == null || !carDtos.Any())
            {
                throw new Exception("No data found or invalid JSON format.");
            }

            
            foreach (var dto in carDtos)
            {
                
                var existingCar = await _context.TaxiCars
                    .Include(tc => tc.Fares)
                    .FirstOrDefaultAsync(tc => tc.LicensePlate == dto.LicensePlate);

                
                var allFares = dto.GetAllFares()
                                  .Select(fd => new Fare
                                  {
                                      From = fd.From,
                                      To = fd.To,
                                      Distance = fd.Distance,
                                      PaidAmount = fd.PaidAmount,
                                      FareStartDate = fd.FareStartDate
                                      
                                  })
                                  .ToList();

                if (existingCar != null)
                {
                    
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
                    
                    var newCar = new TaxiCar
                    {
                        LicensePlate = dto.LicensePlate,
                        Driver = dto.Driver,
                        Model = "Unknown",  
                        TotalDistance = 0,  
                        DistanceSinceLastMaintenance = 0
                    };

                    
                    newCar.Fares = allFares;
                    await _context.TaxiCars.AddAsync(newCar);
                }
            }

            
            await _context.SaveChangesAsync();
        }

        private async Task ImportXmlDataAsync(string filePath)
        {
            try
            {
                
                var serializer = new XmlSerializer(typeof(CarSharingDto));
                
                
                using (var reader = new StreamReader(filePath))
                {
                    
                    var carSharing = (CarSharingDto)serializer.Deserialize(reader);
                    
                    
                    if (carSharing.Cars != null && carSharing.Cars.Items != null)
                    {
                        foreach (var car in carSharing.Cars.Items)
                        {
                            var existingCar = await _context.TaxiCars
                                .FirstOrDefaultAsync(c => c.Id == car.Id || c.LicensePlate == car.LicensePlate);
                                
                            if (existingCar == null)
                            {
                                
                                if (string.IsNullOrEmpty(car.LicensePlate))
                                {
                                    car.LicensePlate = $"CAR-{car.Id}";
                                }
                                
                                
                                if (string.IsNullOrEmpty(car.Driver))
                                {
                                    car.Driver = $"Driver-{car.Id}";
                                }
                                
                                await _context.TaxiCars.AddAsync(car);
                            }
                            else
                            {
                                
                                existingCar.Model = car.Model;
                                existingCar.TotalDistance = car.TotalDistance;
                                existingCar.DistanceSinceLastMaintenance = car.DistanceSinceLastMaintenance;
                                
                                
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
