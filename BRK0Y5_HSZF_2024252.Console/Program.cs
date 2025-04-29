using BRK0Y5_HSZF_2024252.Application.Interfaces;
using BRK0Y5_HSZF_2024252.Application.Services;
using BRK0Y5_HSZF_2024252.Persistence.MsSql;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using BRK0Y5_HSZF_2024252.Model;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Text;

class Program
{
    static async Task Main(string[] args)
    {
        try
        {
            Console.WriteLine("Starting Car Sharing System...");
            // Build a Host that includes configuration, logging, and DI (dependency injection).
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    // Load appsettings.json, if present
                    config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                })
                .ConfigureLogging((hostingContext, logging) =>
                {
                    // Configure the logging system
                    logging.ClearProviders();
                    logging.AddConsole();
                    logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                })
                .ConfigureServices((context, services) =>
                {
                    // Register the DbContext with SQLite, enabling lazy loading if you want
                    services.AddDbContext<TaxiDbContext>(options =>
                        options.UseSqlite("Data Source=CarSharing.db")
                               .UseLazyLoadingProxies()
                    );

                    // Register application services
                    services.AddScoped<ICarManagementService, CarManagementService>();
                    services.AddScoped<IDataImporterService, DataImporterService>();
                    services.AddScoped<IStatisticsService, StatisticsService>();
                })
                .Build();

            Console.WriteLine("Applying database migrations...");
            // Apply migrations on startup
            using (var scope = host.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<TaxiDbContext>();
                dbContext.Database.Migrate();
            }

            // Run the main menu loop
            await RunApplicationAsync(host.Services);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: An unhandled exception occurred: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }

    /// <summary>
    /// The main menu / interactive loop that receives user input and calls the relevant services.
    /// </summary>
    static async Task RunApplicationAsync(IServiceProvider services)
    {
        try
        {
            var dataImporter = services.GetRequiredService<IDataImporterService>();
            var carManager = services.GetRequiredService<ICarManagementService>();
            var statisticsService = services.GetRequiredService<IStatisticsService>();
            
            // Register event handlers for car manager
            if (carManager is CarManagementService carSharingService)
            {
                carSharingService.TripStarted += OnTripStarted;
                carSharingService.TripFinished += OnTripFinished;
                carSharingService.MaintenanceRequested += OnMaintenanceRequested;
                carSharingService.InsufficientFunds += OnInsufficientFunds;
            }

            bool exit = false;
            while (!exit)
            {
                Console.WriteLine("\n------------------------");
                Console.WriteLine("Car Sharing System");
                Console.WriteLine("1. Import XML file");
                Console.WriteLine("2. Manage cars");
                Console.WriteLine("3. Manage customers");
                Console.WriteLine("4. Trip operations");
                Console.WriteLine("5. View statistics");
                Console.WriteLine("6. Export to CSV");
                Console.WriteLine("7. Exit");
                Console.WriteLine("------------------------");
                Console.Write("Choose an option: ");

                var input = Console.ReadLine();
                switch (input)
                {
                    case "1":
                        await ImportXmlAsync(dataImporter);
                        break;
                    case "2":
                        await ManageCarsAsync(carManager);
                        break;
                    case "3":
                        await ManageCustomersAsync(carManager);
                        break;
                    case "4":
                        await TripOperationsAsync(carManager);
                        break;
                    case "5":
                        await ViewStatisticsAsync(statisticsService, carManager);
                        break;
                    case "6":
                        await ExportToCSVAsync(carManager);
                        break;
                    case "7":
                        exit = true;
                        break;
                    default:
                        Console.WriteLine("Invalid option.");
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: An exception occurred in the application: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
        finally
        {
            Console.WriteLine("\nApplication closed. Press any key to exit...");
            Console.ReadKey();
        }
    }
    
    #region Event Handlers
    
    private static void OnTripStarted(object sender, TripEventArgs e)
    {
        Console.WriteLine($"\nTrip started:");
        Console.WriteLine($"Car: {e.CarModel} (ID: {e.CarId}, License: {e.LicensePlate})");
        Console.WriteLine($"Customer: {e.CustomerName} (ID: {e.CustomerId})");
        Console.WriteLine($"Estimated distance: {e.Distance:F2} km");
        Console.WriteLine($"Estimated cost: {e.Cost:F2}€");
        Console.WriteLine("\nPress any key to continue...");
        Console.ReadKey();
    }
    
    private static void OnTripFinished(object sender, TripEventArgs e)
    {
        Console.WriteLine($"\nTrip finished:");
        Console.WriteLine($"Car: {e.CarModel} (ID: {e.CarId}, License: {e.LicensePlate})");
        Console.WriteLine($"Customer: {e.CustomerName} (ID: {e.CustomerId})");
        Console.WriteLine($"Distance: {e.Distance:F2} km");
        Console.WriteLine($"Cost: {e.Cost:F2}€");
        Console.WriteLine("\nPress any key to continue...");
        Console.ReadKey();
    }
    
    private static void OnMaintenanceRequested(object sender, MaintenanceEventArgs e)
    {
        Console.WriteLine($"\nMaintenance performed:");
        Console.WriteLine($"Car: {e.CarModel} (ID: {e.CarId}, License: {e.LicensePlate})");
        Console.WriteLine($"Total distance: {e.TotalDistance:F2} km");
        Console.WriteLine("\nPress any key to continue...");
        Console.ReadKey();
    }
    
    private static void OnInsufficientFunds(object sender, InsufficientFundsEventArgs e)
    {
        Console.WriteLine($"\nInsufficient funds:");
        Console.WriteLine($"Customer: {e.CustomerName} (ID: {e.CustomerId})");
        Console.WriteLine($"Current balance: {e.CurrentBalance:F2}€");
        Console.WriteLine($"Minimum required: {e.MinimumBalance:F2}€");
        Console.WriteLine("\nPress any key to continue...");
        Console.ReadKey();
    }
    
    #endregion

    /// <summary>
    /// 1) Prompt for an XML file path, then call IDataImporterService to load data.
    /// </summary>
    static async Task ImportXmlAsync(IDataImporterService dataImporter)
    {
        Console.Write("Enter XML file path: ");
        var filePath = Console.ReadLine();

        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            Console.WriteLine("Invalid file path.");
            return;
        }

        try
        {
            await dataImporter.ImportDataAsync(filePath);
            Console.WriteLine("Data successfully imported.");
        }
        catch (FileNotFoundException)
        {
            Console.WriteLine("File not found.");
        }
        catch (JsonException)
        {
            Console.WriteLine("Error parsing XML. Check file format.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unknown error: {ex.Message}");
        }
    }

    /// <summary>
    /// Manage cars submenu.
    /// </summary>
    static async Task ManageCarsAsync(ICarManagementService carManager)
    {
        bool back = false;
        while (!back)
        {
            Console.Clear();
            Console.WriteLine("=== Manage Cars ===");
            Console.WriteLine("1. List all cars");
            Console.WriteLine("2. Add new car");
            Console.WriteLine("3. Update car");
            Console.WriteLine("4. Delete car");
            Console.WriteLine("5. Perform maintenance");
            Console.WriteLine("0. Back to main menu");
            Console.Write("\nSelect an option: ");
            
            var input = Console.ReadLine();
            
            switch (input)
            {
                case "1":
                    await ListCarsAsync(carManager);
                    break;
                case "2":
                    await AddCarAsync(carManager);
                    break;
                case "3":
                    await UpdateCarAsync(carManager);
                    break;
                case "4":
                    await DeleteCarAsync(carManager);
                    break;
                case "5":
                    await PerformMaintenanceAsync(carManager);
                    break;
                case "0":
                    back = true;
                    break;
                default:
                    Console.WriteLine("Invalid option. Press any key to continue...");
                    Console.ReadKey();
                    break;
            }
        }
    }

    /// <summary>
    /// Manage customers submenu.
    /// </summary>
    static async Task ManageCustomersAsync(ICarManagementService carManager)
    {
        bool back = false;
        while (!back)
        {
            Console.Clear();
            Console.WriteLine("=== Manage Customers ===");
            Console.WriteLine("1. List all customers");
            Console.WriteLine("2. Add new customer");
            Console.WriteLine("3. Update customer");
            Console.WriteLine("4. Delete customer");
            Console.WriteLine("0. Back to main menu");
            Console.Write("\nSelect an option: ");
            
            var input = Console.ReadLine();
            
            switch (input)
            {
                case "1":
                    await ListCustomersAsync(carManager);
                    break;
                case "2":
                    await AddCustomerAsync(carManager);
                    break;
                case "3":
                    await UpdateCustomerAsync(carManager);
                    break;
                case "4":
                    await DeleteCustomerAsync(carManager);
                    break;
                case "0":
                    back = true;
                    break;
                default:
                    Console.WriteLine("Invalid option. Press any key to continue...");
                    Console.ReadKey();
                    break;
            }
        }
    }

    /// <summary>
    /// Trip operations submenu.
    /// </summary>
    static async Task TripOperationsAsync(ICarManagementService carManager)
    {
        bool back = false;
        while (!back)
        {
            Console.Clear();
            Console.WriteLine("=== Trip Operations ===");
            Console.WriteLine("1. List all trips");
            Console.WriteLine("2. Start a trip");
            Console.WriteLine("3. Finish a trip");
            Console.WriteLine("0. Back to main menu");
            Console.Write("\nSelect an option: ");
            
            var input = Console.ReadLine();
            
            switch (input)
            {
                case "1":
                    await ListTripsAsync(carManager);
                    break;
                case "2":
                    await StartTripAsync(carManager);
                    break;
                case "3":
                    await FinishTripAsync(carManager);
                    break;
                case "0":
                    back = true;
                    break;
                default:
                    Console.WriteLine("Invalid option. Press any key to continue...");
                    Console.ReadKey();
                    break;
            }
        }
    }

    /// <summary>
    /// View various statistics.
    /// </summary>
    static async Task ViewStatisticsAsync(IStatisticsService statisticsService, ICarManagementService carManager)
    {
        bool back = false;
        while (!back)
        {
            Console.Clear();
            Console.WriteLine("=== Statistics ===");
            Console.WriteLine("1. Generate full statistics report");
            Console.WriteLine("2. Most used car");
            Console.WriteLine("3. Top 10 paying customers");
            Console.WriteLine("4. Average car distance");
            Console.WriteLine("0. Back to main menu");
            Console.Write("\nSelect an option: ");
            
            var input = Console.ReadLine();
            
            switch (input)
            {
                case "1":
                    await statisticsService.GenerateStatisticsAsync();
                    Console.WriteLine("Statistics generated to files. Press any key to continue...");
                    Console.ReadKey();
                    break;
                case "2":
                    await ShowMostUsedCarAsync(carManager);
                    break;
                case "3":
                    await ShowTopCustomersAsync(carManager);
                    break;
                case "4":
                    await ShowAverageDistanceAsync(carManager);
                    break;
                case "0":
                    back = true;
                    break;
                default:
                    Console.WriteLine("Invalid option. Press any key to continue...");
                    Console.ReadKey();
                    break;
            }
        }
    }

    /// <summary>
    /// Export to CSV submenu.
    /// </summary>
    static async Task ExportToCSVAsync(ICarManagementService carManager)
    {
        bool back = false;
        while (!back)
        {
            Console.Clear();
            Console.WriteLine("=== Export to CSV ===");
            Console.WriteLine("1. Export cars");
            Console.WriteLine("2. Export customers");
            Console.WriteLine("3. Export trips");
            Console.WriteLine("4. Export all");
            Console.WriteLine("0. Back to main menu");
            Console.Write("\nSelect an option: ");
            
            var input = Console.ReadLine();
            
            switch (input)
            {
                case "1":
                    await carManager.ExportCarsToCSVAsync();
                    Console.WriteLine("Cars exported to cars.csv. Press any key to continue...");
                    Console.ReadKey();
                    break;
                case "2":
                    await carManager.ExportCustomersToCSVAsync();
                    Console.WriteLine("Customers exported to customers.csv. Press any key to continue...");
                    Console.ReadKey();
                    break;
                case "3":
                    await carManager.ExportTripsToCSVAsync();
                    Console.WriteLine("Trips exported to trips.csv. Press any key to continue...");
                    Console.ReadKey();
                    break;
                case "4":
                    await carManager.ExportCarsToCSVAsync();
                    await carManager.ExportCustomersToCSVAsync();
                    await carManager.ExportTripsToCSVAsync();
                    Console.WriteLine("All data exported to CSV files. Press any key to continue...");
                    Console.ReadKey();
                    break;
                case "0":
                    back = true;
                    break;
                default:
                    Console.WriteLine("Invalid option. Press any key to continue...");
                    Console.ReadKey();
                    break;
            }
        }
    }

    #region Helper Functions
    
    static async Task ListCarsAsync(ICarManagementService carManager)
    {
        Console.Clear();
        Console.WriteLine("=== All Cars ===");
        
        var cars = await carManager.GetCarsAsync();
        
        if (cars.Count == 0)
        {
            Console.WriteLine("No cars found.");
        }
        else
        {
            foreach (var car in cars)
            {
                Console.WriteLine($"ID: {car.Id}, License: {car.LicensePlate}, Model: {car.Model}, " +
                                $"Driver: {car.Driver}, Total Distance: {car.TotalDistance:F2} km, " +
                                $"Distance Since Maintenance: {car.DistanceSinceLastMaintenance:F2} km");
            }
        }
        
        Console.WriteLine("\nPress any key to continue...");
        Console.ReadKey();
    }
    
    static async Task AddCarAsync(ICarManagementService carManager)
    {
        Console.Clear();
        Console.WriteLine("=== Add New Car ===");
        
        Console.Write("Enter license plate: ");
        var licensePlate = Console.ReadLine();
        
        Console.Write("Enter model: ");
        var model = Console.ReadLine();
        
        Console.Write("Enter driver name: ");
        var driver = Console.ReadLine();
        
        var car = new TaxiCar
        {
            LicensePlate = licensePlate,
            Model = model,
            Driver = driver,
            TotalDistance = 0,
            DistanceSinceLastMaintenance = 0
        };
        
        await carManager.AddCarAsync(car);
        Console.WriteLine("Car added successfully. Press any key to continue...");
        Console.ReadKey();
    }
    
    static async Task UpdateCarAsync(ICarManagementService carManager)
    {
        Console.Clear();
        Console.WriteLine("=== Update Car ===");
        
        Console.Write("Enter license plate of car to update: ");
        var licensePlate = Console.ReadLine();
        
        var car = await carManager.GetCarByLicensePlateAsync(licensePlate);
        if (car == null)
        {
            Console.WriteLine("Car not found. Press any key to continue...");
            Console.ReadKey();
            return;
        }
        
        Console.WriteLine($"Updating car: {car.Model} (License: {car.LicensePlate})");
        
        Console.Write($"Enter new model (current: {car.Model}) or press Enter to keep current: ");
        var model = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(model))
        {
            car.Model = model;
        }
        
        Console.Write($"Enter new driver name (current: {car.Driver}) or press Enter to keep current: ");
        var driver = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(driver))
        {
            car.Driver = driver;
        }
        
        await carManager.UpdateCarAsync(car);
        Console.WriteLine("Car updated successfully. Press any key to continue...");
        Console.ReadKey();
    }
    
    static async Task DeleteCarAsync(ICarManagementService carManager)
    {
        Console.Clear();
        Console.WriteLine("=== Delete Car ===");
        
        Console.Write("Enter license plate of car to delete: ");
        var licensePlate = Console.ReadLine();
        
        var car = await carManager.GetCarByLicensePlateAsync(licensePlate);
        if (car == null)
        {
            Console.WriteLine("Car not found. Press any key to continue...");
            Console.ReadKey();
            return;
        }
        
        Console.WriteLine($"Are you sure you want to delete car: {car.Model} (License: {car.LicensePlate})? (y/n)");
        var confirmation = Console.ReadLine()?.ToLower();
        
        if (confirmation == "y" || confirmation == "yes")
        {
            await carManager.DeleteCarAsync(licensePlate);
            Console.WriteLine("Car deleted successfully. Press any key to continue...");
        }
        else
        {
            Console.WriteLine("Deletion cancelled. Press any key to continue...");
        }
        
        Console.ReadKey();
    }
    
    static async Task PerformMaintenanceAsync(ICarManagementService carManager)
    {
        Console.Clear();
        Console.WriteLine("=== Perform Maintenance ===");
        
        Console.Write("Enter license plate of car: ");
        var licensePlate = Console.ReadLine();
        
        var car = await carManager.GetCarByLicensePlateAsync(licensePlate);
        if (car == null)
        {
            Console.WriteLine("Car not found. Press any key to continue...");
            Console.ReadKey();
            return;
        }
        
        await carManager.PerformMaintenanceAsync(licensePlate);
        // Event handler will show confirmation
    }
    
    static async Task ListCustomersAsync(ICarManagementService carManager)
    {
        Console.Clear();
        Console.WriteLine("=== All Customers ===");
        
        var customers = await carManager.GetCustomersAsync();
        
        if (customers.Count == 0)
        {
            Console.WriteLine("No customers found.");
        }
        else
        {
            foreach (var customer in customers)
            {
                Console.WriteLine($"ID: {customer.Id}, Name: {customer.Name}, Balance: {customer.Balance:F2}€");
            }
        }
        
        Console.WriteLine("\nPress any key to continue...");
        Console.ReadKey();
    }
    
    static async Task AddCustomerAsync(ICarManagementService carManager)
    {
        Console.Clear();
        Console.WriteLine("=== Add New Customer ===");
        
        Console.Write("Enter name: ");
        var name = Console.ReadLine();
        
        Console.Write("Enter initial balance (€): ");
        if (!decimal.TryParse(Console.ReadLine(), out decimal balance))
        {
            Console.WriteLine("Invalid balance. Press any key to continue...");
            Console.ReadKey();
            return;
        }
        
        var customer = new Customer
        {
            Name = name,
            Balance = balance
        };
        
        await carManager.AddCustomerAsync(customer);
        Console.WriteLine("Customer added successfully. Press any key to continue...");
        Console.ReadKey();
    }
    
    static async Task UpdateCustomerAsync(ICarManagementService carManager)
    {
        Console.Clear();
        Console.WriteLine("=== Update Customer ===");
        
        Console.Write("Enter customer ID: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        {
            Console.WriteLine("Invalid ID. Press any key to continue...");
            Console.ReadKey();
            return;
        }
        
        var customer = await carManager.GetCustomerByIdAsync(id);
        if (customer == null)
        {
            Console.WriteLine("Customer not found. Press any key to continue...");
            Console.ReadKey();
            return;
        }
        
        Console.WriteLine($"Updating customer: {customer.Name} (ID: {customer.Id})");
        
        Console.Write($"Enter new name (current: {customer.Name}) or press Enter to keep current: ");
        var name = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(name))
        {
            customer.Name = name;
        }
        
        Console.Write($"Enter new balance (current: {customer.Balance:F2}€) or press Enter to keep current: ");
        var balanceStr = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(balanceStr) && decimal.TryParse(balanceStr, out decimal balance))
        {
            customer.Balance = balance;
        }
        
        await carManager.UpdateCustomerAsync(customer);
        Console.WriteLine("Customer updated successfully. Press any key to continue...");
        Console.ReadKey();
    }
    
    static async Task DeleteCustomerAsync(ICarManagementService carManager)
    {
        Console.Clear();
        Console.WriteLine("=== Delete Customer ===");
        
        Console.Write("Enter customer ID: ");
        if (!int.TryParse(Console.ReadLine(), out int id))
        {
            Console.WriteLine("Invalid ID. Press any key to continue...");
            Console.ReadKey();
            return;
        }
        
        var customer = await carManager.GetCustomerByIdAsync(id);
        if (customer == null)
        {
            Console.WriteLine("Customer not found. Press any key to continue...");
            Console.ReadKey();
            return;
        }
        
        Console.WriteLine($"Are you sure you want to delete customer: {customer.Name} (ID: {customer.Id})? (y/n)");
        var confirmation = Console.ReadLine()?.ToLower();
        
        if (confirmation == "y" || confirmation == "yes")
        {
            await carManager.DeleteCustomerAsync(id);
            Console.WriteLine("Customer deleted successfully. Press any key to continue...");
        }
        else
        {
            Console.WriteLine("Deletion cancelled. Press any key to continue...");
        }
        
        Console.ReadKey();
    }
    
    static async Task ListTripsAsync(ICarManagementService carManager)
    {
        // For simplicity, we'll list all fares
        var allCars = await carManager.GetCarsAsync();
        var allTrips = new List<Fare>();
        
        foreach (var car in allCars)
        {
            allTrips.AddRange(car.Fares);
        }
        
        Console.Clear();
        Console.WriteLine("=== All Trips ===");
        
        if (allTrips.Count == 0)
        {
            Console.WriteLine("No trips found.");
        }
        else
        {
            foreach (var trip in allTrips)
            {
                var car = allCars.FirstOrDefault(c => c.Id == trip.CarId);
                Console.WriteLine($"ID: {trip.Id}, Car: {car?.Model} (License: {car?.LicensePlate}), " +
                                $"Customer ID: {trip.CustomerId}, Distance: {trip.Distance:F2} km, " +
                                $"Cost: {trip.PaidAmount:F2}€, Date: {trip.FareStartDate}");
            }
        }
        
        Console.WriteLine("\nPress any key to continue...");
        Console.ReadKey();
    }
    
    static async Task StartTripAsync(ICarManagementService carManager)
    {
        Console.Clear();
        Console.WriteLine("=== Start Trip ===");
        
        // List available cars
        Console.WriteLine("Available cars:");
        var cars = await carManager.GetCarsAsync();
        foreach (var car in cars)
        {
            Console.WriteLine($"License: {car.LicensePlate}, Model: {car.Model}, " +
                            $"Driver: {car.Driver}, Distance since maintenance: {car.DistanceSinceLastMaintenance:F2} km");
        }
        
        // List customers
        Console.WriteLine("\nCustomers:");
        var customers = await carManager.GetCustomersAsync();
        foreach (var customer in customers)
        {
            Console.WriteLine($"ID: {customer.Id}, Name: {customer.Name}, Balance: {customer.Balance:F2}€");
        }
        
        // Get input
        Console.Write("\nEnter car license plate: ");
        var licensePlate = Console.ReadLine();
        
        Console.Write("Enter customer ID: ");
        if (!int.TryParse(Console.ReadLine(), out int customerId))
        {
            Console.WriteLine("Invalid customer ID. Press any key to continue...");
            Console.ReadKey();
            return;
        }
        
        Console.Write("Enter estimated distance (km): ");
        if (!double.TryParse(Console.ReadLine(), out double distance))
        {
            Console.WriteLine("Invalid distance. Press any key to continue...");
            Console.ReadKey();
            return;
        }
        
        var success = await carManager.StartTripAsync(licensePlate, customerId, distance);
        if (!success)
        {
            Console.WriteLine("Failed to start trip. Check if car exists, customer exists, and customer has sufficient funds.");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }
        // Event handler will show confirmation if successful
    }
    
    static async Task FinishTripAsync(ICarManagementService carManager)
    {
        Console.Clear();
        Console.WriteLine("=== Finish Trip ===");
        
        // Get input
        Console.Write("Enter car license plate: ");
        var licensePlate = Console.ReadLine();
        
        Console.Write("Enter customer ID: ");
        if (!int.TryParse(Console.ReadLine(), out int customerId))
        {
            Console.WriteLine("Invalid customer ID. Press any key to continue...");
            Console.ReadKey();
            return;
        }
        
        Console.Write("Enter actual distance (km): ");
        if (!double.TryParse(Console.ReadLine(), out double distance))
        {
            Console.WriteLine("Invalid distance. Press any key to continue...");
            Console.ReadKey();
            return;
        }
        
        await carManager.FinishTripAsync(licensePlate, customerId, distance);
        // Event handler will show confirmation
    }
    
    static async Task ShowMostUsedCarAsync(ICarManagementService carManager)
    {
        Console.Clear();
        Console.WriteLine("=== Most Used Car ===");
        
        var car = await carManager.GetMostUsedCarAsync();
        
        if (car == null)
        {
            Console.WriteLine("No cars found.");
        }
        else
        {
            Console.WriteLine($"Most used car: {car.Model} (License: {car.LicensePlate})");
            Console.WriteLine($"Total distance: {car.TotalDistance:F2} km");
            Console.WriteLine($"Distance since last maintenance: {car.DistanceSinceLastMaintenance:F2} km");
        }
        
        Console.WriteLine("\nPress any key to continue...");
        Console.ReadKey();
    }
    
    static async Task ShowTopCustomersAsync(ICarManagementService carManager)
    {
        Console.Clear();
        Console.WriteLine("=== Top 10 Paying Customers ===");
        
        var customers = await carManager.GetTopPayingCustomersAsync();
        
        if (customers.Count == 0)
        {
            Console.WriteLine("No customers found.");
        }
        else
        {
            for (int i = 0; i < customers.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {customers[i].Name} (ID: {customers[i].Id})");
            }
        }
        
        Console.WriteLine("\nPress any key to continue...");
        Console.ReadKey();
    }
    
    static async Task ShowAverageDistanceAsync(ICarManagementService carManager)
    {
        Console.Clear();
        Console.WriteLine("=== Average Car Distance ===");
        
        double avgDistance = await carManager.GetAverageCarDistanceAsync();
        
        Console.WriteLine($"Average distance across all cars: {avgDistance:F2} km");
        
        Console.WriteLine("\nPress any key to continue...");
        Console.ReadKey();
    }
    
    #endregion
}