using ABC123_HSZF_2024252.Application.Interfaces;
using ABC123_HSZF_2024252.Application.Services;
using ABC123_HSZF_2024252.Persistence.MsSql;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using ABC123_HSZF_2024252.Model;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Text;

class Program
{
    static async Task Main(string[] args)
    {
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
                    options.UseSqlite("Data Source=TaxiDatabase.db")
                           .UseLazyLoadingProxies()
                );

                // Register application services
                services.AddScoped<ICarManagementService, CarManagementService>();
                services.AddScoped<IDataImporterService, DataImporterService>();
                services.AddScoped<IStatisticsService, StatisticsService>();
            })
            .Build();

        // Apply migrations on startup
        using (var scope = host.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<TaxiDbContext>();
            dbContext.Database.Migrate();
        }

        // Run the main menu loop
        await RunApplicationAsync(host.Services);
    }

    /// <summary>
    /// The main menu / interactive loop that receives user input and calls the relevant services.
    /// </summary>
    static async Task RunApplicationAsync(IServiceProvider services)
    {
        var dataImporter = services.GetRequiredService<IDataImporterService>();
        var carManager = services.GetRequiredService<ICarManagementService>();
        var statisticsService = services.GetRequiredService<IStatisticsService>();

        bool exit = false;
        while (!exit)
        {
            Console.WriteLine("\n------------------------");
            Console.WriteLine("Taxi Management System");
            Console.WriteLine("1. Import JSON file");
            Console.WriteLine("2. List cars");
            Console.WriteLine("3. Add new car");
            Console.WriteLine("4. Update car");
            Console.WriteLine("5. Delete car");
            Console.WriteLine("6. Add fare/trip");
            Console.WriteLine("7. Generate statistics");
            Console.WriteLine("8. Exit");
            Console.WriteLine("9. (Optional) Export cars to CSV");
            Console.WriteLine("------------------------");
            Console.Write("Choose an option: ");

            var input = Console.ReadLine();
            switch (input)
            {
                case "1":
                    await ImportJsonAsync(dataImporter);
                    break;
                case "2":
                    await ListCarsAsync(carManager);
                    break;
                case "3":
                    await AddCarAsync(carManager);
                    break;
                case "4":
                    await UpdateCarAsync(carManager);
                    break;
                case "5":
                    await DeleteCarAsync(carManager);
                    break;
                case "6":
                    await AddFareAsync(carManager);
                    break;
                case "7":
                    await GenerateStatisticsAsync(statisticsService);
                    break;
                case "8":
                    exit = true;
                    break;
                case "9":
                    await ExportCarsToCsvAsync(carManager);
                    break;
                default:
                    Console.WriteLine("Invalid option.");
                    break;
            }
        }
    }

    /// <summary>
    /// 1) Prompt for a JSON file path, then call IDataImporterService to load data.
    /// </summary>
    static async Task ImportJsonAsync(IDataImporterService dataImporter)
    {
        Console.Write("Enter JSON file path: ");
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
            Console.WriteLine("Error parsing JSON. Check file format.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unknown error: {ex.Message}");
        }
    }

    /// <summary>
    /// 2) Prompt for optional search keywords, list matching cars and their fares.
    /// </summary>
    static async Task ListCarsAsync(ICarManagementService carManager)
    {
        Console.WriteLine("\nEnter search criteria (or just press Enter to skip):");

        Console.Write("License plate: ");
        var licensePlate = Console.ReadLine();

        Console.Write("Driver name: ");
        var driver = Console.ReadLine();

        try
        {
            var cars = await carManager.SearchCarsAsync(licensePlate, driver);
            if (!cars.Any())
            {
                Console.WriteLine("No matching cars found.");
                return;
            }

            Console.WriteLine("\nMatching cars:");
            foreach (var c in cars)
            {
                Console.WriteLine($"LicensePlate={c.LicensePlate}, Driver={c.Driver}");
                if (c.Fares.Any())
                {
                    Console.WriteLine("  Fares:");
                    foreach (var f in c.Fares)
                    {
                        Console.WriteLine($"    {f.From} -> {f.To}, Distance={f.Distance}, Cost={f.PaidAmount}");
                    }
                }
                else
                {
                    Console.WriteLine("  No fares/trips.");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error listing cars: {ex.Message}");
        }
    }

    /// <summary>
    /// 3) Prompt user for new car details, then add to DB.
    /// </summary>
    static async Task AddCarAsync(ICarManagementService carManager)
    {
        Console.Write("Enter license plate: ");
        var plate = Console.ReadLine();

        Console.Write("Enter driver name: ");
        var driver = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(plate) || string.IsNullOrWhiteSpace(driver))
        {
            Console.WriteLine("License plate and driver name required.");
            return;
        }

        var newCar = new TaxiCar
        {
            LicensePlate = plate,
            Driver = driver
        };

        await carManager.AddCarAsync(newCar);
        Console.WriteLine("New car added.");
    }

    /// <summary>
    /// 4) Fetch car by plate, prompt for new driver name, update in DB.
    /// </summary>
    static async Task UpdateCarAsync(ICarManagementService carManager)
    {
        Console.Write("Enter the license plate of the car to update: ");
        var plate = Console.ReadLine();

        var car = await carManager.GetCarByLicensePlateAsync(plate);
        if (car == null)
        {
            Console.WriteLine("Car not found.");
            return;
        }

        Console.Write("Enter new driver name: ");
        var newDriver = Console.ReadLine();

        car.Driver = newDriver;
        await carManager.UpdateCarAsync(car);

        Console.WriteLine("Car updated.");
    }

    /// <summary>
    /// 5) Prompt for plate, delete car if found.
    /// </summary>
    static async Task DeleteCarAsync(ICarManagementService carManager)
    {
        Console.Write("Enter license plate to delete: ");
        var plate = Console.ReadLine();

        await carManager.DeleteCarAsync(plate);
        Console.WriteLine("Car deleted.");
    }

    /// <summary>
    /// 6) Prompt for fare/trip details, attach to the specified car. 
    ///    Optionally, a notification callback logs warnings if cost is abnormally high, etc.
    /// </summary>
    static async Task AddFareAsync(ICarManagementService carManager)
    {
        Console.Write("License plate: ");
        var plate = Console.ReadLine();

        Console.Write("From (start): ");
        var from = Console.ReadLine();

        Console.Write("To (destination): ");
        var to = Console.ReadLine();

        Console.Write("Distance (km): ");
        if (!double.TryParse(Console.ReadLine(), out var distance))
        {
            Console.WriteLine("Invalid distance.");
            return;
        }

        Console.Write("Cost: ");
        if (!decimal.TryParse(Console.ReadLine(), out var cost))
        {
            Console.WriteLine("Invalid cost.");
            return;
        }

        var fare = new Fare
        {
            From = from,
            To = to,
            Distance = distance,
            PaidAmount = cost,
            FareStartDate = DateTime.UtcNow
        };

        try
        {
            await carManager.AddFareAsync(plate, fare, message => Console.WriteLine($"Notification: {message}"));
            Console.WriteLine("Fare added.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error adding fare: {ex.Message}");
        }
    }

    /// <summary>
    /// 7) Calls the statistics service to generate stats and write them to file.
    /// </summary>
    static async Task GenerateStatisticsAsync(IStatisticsService statisticsService)
    {
        try
        {
            await statisticsService.GenerateStatisticsAsync();
            Console.WriteLine("Statistics generated and saved to TaxiStatistics.txt.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error generating statistics: {ex.Message}");
        }
    }

    /// <summary>
    /// 9) Optional enhancement: export all cars to a CSV file.
    /// </summary>
    static async Task ExportCarsToCsvAsync(ICarManagementService carManager)
    {
        try
        {
            var cars = await carManager.GetCarsAsync();
            if (!cars.Any())
            {
                Console.WriteLine("No cars to export.");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("LicensePlate,Driver,FareCount");

            foreach (var c in cars)
            {
                sb.AppendLine($"{c.LicensePlate},{c.Driver},{c.Fares.Count}");
            }

            var filename = "cars_export.csv";
            await File.WriteAllTextAsync(filename, sb.ToString());
            Console.WriteLine($"Cars exported to {filename}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error exporting CSV: {ex.Message}");
        }
    }
}