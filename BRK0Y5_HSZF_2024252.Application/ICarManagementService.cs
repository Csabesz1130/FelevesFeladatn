using BRK0Y5_HSZF_2024252.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BRK0Y5_HSZF_2024252.Application.Interfaces
{
    public interface ICarManagementService
    {
        // Original methods
        Task<List<TaxiCar>> GetCarsAsync();
        Task<TaxiCar> GetCarByLicensePlateAsync(string licensePlate);
        Task AddCarAsync(TaxiCar car);
        Task UpdateCarAsync(TaxiCar car);
        Task DeleteCarAsync(string licensePlate);
        Task AddFareAsync(string licensePlate, Fare fare, Action<string> notification = null);
        Task<List<TaxiCar>> SearchCarsAsync(string licensePlate = null, string driver = null);
        
        // New car sharing methods
        Task PerformMaintenanceAsync(string licensePlate);
        Task<TaxiCar> GetCarByIdAsync(int id);
        
        // Customer methods
        Task<List<Customer>> GetCustomersAsync();
        Task<Customer> GetCustomerByIdAsync(int id);
        Task<Customer> GetCustomerByNameAsync(string name);
        Task AddCustomerAsync(Customer customer);
        Task UpdateCustomerAsync(Customer customer);
        Task DeleteCustomerAsync(int id);
        
        // Trip methods
        Task<bool> StartTripAsync(string licensePlate, int customerId, double estimatedDistance);
        Task FinishTripAsync(string licensePlate, int customerId, double actualDistance);
        
        // Queries
        Task<TaxiCar> GetMostUsedCarAsync();
        Task<List<Customer>> GetTopPayingCustomersAsync(int count = 10);
        Task<double> GetAverageCarDistanceAsync();
        
        // Export methods
        Task ExportCarsToCSVAsync(string filePath = "cars.csv");
        Task ExportCustomersToCSVAsync(string filePath = "customers.csv");
        Task ExportTripsToCSVAsync(string filePath = "trips.csv");
    }
}