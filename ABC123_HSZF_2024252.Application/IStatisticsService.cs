using System.Threading.Tasks;

namespace ABC123_HSZF_2024252.Application.Interfaces
{
    /// <summary>
    /// Defines various methods to generate or fetch statistics
    /// about cars, fares, distances, etc.
    /// </summary>
    public interface IStatisticsService
    {
        /// <summary>
        /// Example method to generate statistics (e.g. short trips, average distance)
        /// and write them to an output file or console.
        /// </summary>
        Task GenerateStatisticsAsync();
    }
}
