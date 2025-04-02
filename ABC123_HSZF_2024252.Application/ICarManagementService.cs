using ABC123_HSZF_2024252.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ABC123_HSZF_2024252.Application.Interfaces
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
