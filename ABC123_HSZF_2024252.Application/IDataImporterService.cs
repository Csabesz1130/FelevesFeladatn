using System.Threading.Tasks;

namespace ABC123_HSZF_2024252.Application.Interfaces
{
    /// <summary>
    /// Defines methods for importing data (e.g. from JSON files).
    /// </summary>
    public interface IDataImporterService
    {
        /// <summary>
        /// Reads and imports data from the specified file path into the database.
        /// </summary>
        /// <param name="filePath">Path to the data file (JSON/XML).</param>
        Task ImportDataAsync(string filePath);
    }
}
