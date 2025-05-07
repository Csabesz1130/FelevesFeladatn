using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.EntityFrameworkCore;
using BRK0Y5_HSZF_2024252.Application.Services;
using BRK0Y5_HSZF_2024252.Model;
using BRK0Y5_HSZF_2024252.Persistence.MsSql;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System;
using System.IO;
using System.Xml.Serialization;

namespace BRK0Y5_HSZF_2024252.Test
{
    [TestClass]
    public class CarSharingTests
    {
        private TaxiDbContext _context;
        private CarManagementService _service;
        private bool _tripStartedEventFired;
        private bool _tripFinishedEventFired;
        private bool _maintenanceRequestedEventFired;
        private bool _insufficientFundsEventFired;

        [TestInitialize]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<TaxiDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestCarSharing_{Guid.NewGuid()}")
                .Options;

            _context = new TaxiDbContext(options);
            _service = new CarManagementService(_context);

            _tripStartedEventFired = false;
            _tripFinishedEventFired = false;
            _maintenanceRequestedEventFired = false;
            _insufficientFundsEventFired = false;

            _service.TripStarted += (sender, e) => _tripStartedEventFired = true;
            _service.TripFinished += (sender, e) => _tripFinishedEventFired = true;
            _service.MaintenanceRequested += (sender, e) => _maintenanceRequestedEventFired = true;
            _service.InsufficientFunds += (sender, e) => _insufficientFundsEventFired = true;
        }

        [TestCleanup]
        public void Cleanup()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [TestMethod]
        public async Task AddCar_ShouldAddCarToDatabase()
        {
            var car = new TaxiCar
            {
                LicensePlate = "TEST-123",
                Model = "Test Car",
                Driver = "Test Driver",
                TotalDistance = 0,
                DistanceSinceLastMaintenance = 0
            };

            await _service.AddCarAsync(car);
            var result = await _service.GetCarByLicensePlateAsync("TEST-123");

            Assert.IsNotNull(result);
            Assert.AreEqual("Test Car", result.Model);
            Assert.AreEqual("Test Driver", result.Driver);
        }

        [TestMethod]
        public async Task AddCustomer_ShouldAddCustomerToDatabase()
        {
            var customer = new Customer
            {
                Id = 1,
                Name = "Test Customer",
                Balance = 100
            };

            await _service.AddCustomerAsync(customer);
            var result = await _service.GetCustomerByIdAsync(1);

            Assert.IsNotNull(result);
            Assert.AreEqual("Test Customer", result.Name);
            Assert.AreEqual(100, result.Balance);
        }

        [TestMethod]
        public async Task PerformMaintenance_ShouldResetMaintenanceCounter()
        {
            var car = new TaxiCar
            {
                LicensePlate = "TEST-123",
                Model = "Test Car",
                Driver = "Test Driver",
                TotalDistance = 500,
                DistanceSinceLastMaintenance = 150
            };
            await _service.AddCarAsync(car);

            await _service.PerformMaintenanceAsync("TEST-123");
            var result = await _service.GetCarByLicensePlateAsync("TEST-123");

            Assert.AreEqual(0, result.DistanceSinceLastMaintenance);
            Assert.IsTrue(_maintenanceRequestedEventFired);
        }

        [TestMethod]
        public async Task StartTrip_WithSufficientFunds_ShouldReturnTrue()
        {
            var car = new TaxiCar
            {
                LicensePlate = "TEST-123",
                Model = "Test Car",
                Driver = "Test Driver"
            };
            await _service.AddCarAsync(car);

            var customer = new Customer
            {
                Id = 1,
                Name = "Rich Customer",
                Balance = 100
            };
            await _service.AddCustomerAsync(customer);

            bool result = await _service.StartTripAsync("TEST-123", 1, 50);

            Assert.IsTrue(result);
            Assert.IsTrue(_tripStartedEventFired);
        }

        [TestMethod]
        public async Task StartTrip_WithInsufficientFunds_ShouldReturnFalse()
        {
            var car = new TaxiCar
            {
                LicensePlate = "TEST-123",
                Model = "Test Car",
                Driver = "Test Driver"
            };
            await _service.AddCarAsync(car);

            var customer = new Customer
            {
                Id = 1,
                Name = "Poor Customer",
                Balance = 30
            };
            await _service.AddCustomerAsync(customer);

            bool result = await _service.StartTripAsync("TEST-123", 1, 50);

            Assert.IsFalse(result);
            Assert.IsTrue(_insufficientFundsEventFired);
        }

        [TestMethod]
        public async Task FinishTrip_ShouldUpdateCarAndCustomer()
        {

            var car = new TaxiCar
            {
                LicensePlate = "TEST-123",
                Model = "Test Car",
                Driver = "Test Driver",
                TotalDistance = 1000,
                DistanceSinceLastMaintenance = 100
            };
            await _service.AddCarAsync(car);

            var customer = new Customer
            {
                Id = 1,
                Name = "Test Customer",
                Balance = 100
            };
            await _service.AddCustomerAsync(customer);


            await _service.FinishTripAsync("TEST-123", 1, 50);

            var updatedCar = await _service.GetCarByLicensePlateAsync("TEST-123");
            var updatedCustomer = await _service.GetCustomerByIdAsync(1);

            decimal expectedCost = (decimal)(0.5 + (50 * 0.35));

            Assert.AreEqual(1050, updatedCar.TotalDistance);

            bool distanceIsCorrect = updatedCar.DistanceSinceLastMaintenance == 150 ||
                                     (updatedCar.DistanceSinceLastMaintenance == 0 && _maintenanceRequestedEventFired);

            Assert.IsTrue(distanceIsCorrect,
                $"Distance since maintenance should be either 150 or 0, but was {updatedCar.DistanceSinceLastMaintenance}");

            Assert.AreEqual(100 - expectedCost, updatedCustomer.Balance);
            Assert.IsTrue(_tripFinishedEventFired);
        }

        [TestMethod]
        public void CalculateCost_ShouldComputeCorrectAmount()
        {
            double distance = 100;

            decimal cost = Fare.CalculateCost(distance);

            Assert.AreEqual(35.5m, cost);
        }

        [TestMethod]
        public async Task GetMostUsedCar_ShouldReturnCarWithHighestTotalDistance()
        {
            await _context.Database.EnsureDeletedAsync();
            await _context.Database.EnsureCreatedAsync();

            var car1 = new TaxiCar
            {
                LicensePlate = "CAR-1",
                Model = "Model 1",
                TotalDistance = 1000,
                Driver = "Driver 1"  
            };

            var car2 = new TaxiCar
            {
                LicensePlate = "CAR-2",
                Model = "Model 2",
                TotalDistance = 2000,
                Driver = "Driver 2"  
            };

            await _service.AddCarAsync(car1);
            await _service.AddCarAsync(car2);

            var result = await _service.GetMostUsedCarAsync();

            Assert.IsNotNull(result);
            Assert.AreEqual("CAR-2", result.LicensePlate);
            Assert.AreEqual(2000, result.TotalDistance);
        }

        [TestMethod]
        public async Task GetTopPayingCustomers_ShouldReturnCustomersInOrder()
        {
            await _context.Database.EnsureDeletedAsync();
            await _context.Database.EnsureCreatedAsync();

            var car = new TaxiCar
            {
                LicensePlate = "TEST-CAR",
                Model = "Test Car",
                Driver = "Test Driver"
            };
            await _service.AddCarAsync(car);

            var customer1 = new Customer { Id = 1, Name = "Customer 1", Balance = 200 };
            var customer2 = new Customer { Id = 2, Name = "Customer 2", Balance = 300 };
            var customer3 = new Customer { Id = 3, Name = "Customer 3", Balance = 400 };

            await _service.AddCustomerAsync(customer1);
            await _service.AddCustomerAsync(customer2);
            await _service.AddCustomerAsync(customer3);

            await _service.FinishTripAsync("TEST-CAR", 1, 100);
            await _service.FinishTripAsync("TEST-CAR", 2, 200);
            await _service.FinishTripAsync("TEST-CAR", 3, 50);

            var topCustomers = await _service.GetTopPayingCustomersAsync(2);

            Console.WriteLine("Retrieved top customers:");
            foreach (var customer in topCustomers)
            {
                Console.WriteLine($"ID: {customer.Id}, Name: {customer.Name}");
            }

            Assert.IsNotNull(topCustomers);
            Assert.AreEqual(2, topCustomers.Count);
            Assert.AreEqual(2, topCustomers[0].Id);
            Assert.AreEqual(1, topCustomers[1].Id);
        }

        [TestMethod]
        public async Task Trip_WithOver200KmMaintenance_ShouldTriggerMaintenance()
        {
            _maintenanceRequestedEventFired = false;

            var car = new TaxiCar
            {
                LicensePlate = "TEST-CAR",
                Model = "Test Car",
                Driver = "Test Driver",
                TotalDistance = 1000,
                DistanceSinceLastMaintenance = 190
            };
            await _service.AddCarAsync(car);

            var customer = new Customer { Id = 1, Name = "Test Customer", Balance = 200 };
            await _service.AddCustomerAsync(customer);

            await _service.FinishTripAsync("TEST-CAR", 1, 15);

            await _context.Entry(car).ReloadAsync();
            var updatedCar = await _service.GetCarByLicensePlateAsync("TEST-CAR");

            Console.WriteLine($"Maintenance event fired: {_maintenanceRequestedEventFired}");
            Console.WriteLine($"Distance since maintenance: {updatedCar.DistanceSinceLastMaintenance}");

            Assert.IsTrue(_maintenanceRequestedEventFired);
            Assert.AreEqual(0, updatedCar.DistanceSinceLastMaintenance);
        }

        [TestMethod]
        public async Task GetAverageCarDistance_ShouldCalculateCorrectly()
        {
            var car1 = new TaxiCar
            {
                LicensePlate = "CAR-1",
                Model = "Model 1",
                TotalDistance = 1000,
                Driver = "Driver 1"  
            };

            var car2 = new TaxiCar
            {
                LicensePlate = "CAR-2",
                Model = "Model 2",
                TotalDistance = 3000,
                Driver = "Driver 2"  
            };

            await _service.AddCarAsync(car1);
            await _service.AddCarAsync(car2);

            double avgDistance = await _service.GetAverageCarDistanceAsync();

            Assert.AreEqual(2000, avgDistance, 0.01);
        }

        [TestMethod]
        public async Task ExportCarsToCSV_ShouldCreateValidFile()
        {
            var car1 = new TaxiCar
            {
                LicensePlate = "CAR-1",
                Model = "Model 1",
                TotalDistance = 1000,
                DistanceSinceLastMaintenance = 100,
                Driver = "Driver 1"  // Added this line
            };

            var car2 = new TaxiCar
            {
                LicensePlate = "CAR-2",
                Model = "Model 2",
                TotalDistance = 2000,
                DistanceSinceLastMaintenance = 50,
                Driver = "Driver 2"  // Added this line
            };

            await _service.AddCarAsync(car1);
            await _service.AddCarAsync(car2);

            string testDir = Path.Combine(Path.GetTempPath(), "CarSharingTests");
            Directory.CreateDirectory(testDir);
            string tempFile = Path.Combine(testDir, "cars_test.csv");

            try
            {
                await _service.ExportCarsToCSVAsync(tempFile);

                Assert.IsTrue(File.Exists(tempFile));
                string[] lines = File.ReadAllLines(tempFile);

                Assert.IsTrue(lines.Length >= 3);
                Assert.IsTrue(lines[0].Contains("Id"));
                Assert.IsTrue(lines[0].Contains("Model"));
                Assert.IsTrue(lines[0].Contains("TotalDistance"));
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }

                try { Directory.Delete(testDir); } catch { }
            }
        }
    }
}