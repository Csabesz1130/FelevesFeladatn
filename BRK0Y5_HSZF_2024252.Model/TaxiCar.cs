using System;
using System.Collections.Generic;

namespace BRK0Y5_HSZF_2024252.Model
{
    public class TaxiCar
    {
        public int Id { get; set; }
        public string LicensePlate { get; set; }
        public string Driver { get; set; }

        // Example property
        public DateTime LastServiceDate { get; set; } = DateTime.UtcNow;

        // Navigation property: One car, many fares
        public virtual ICollection<Fare> Fares { get; set; } = new List<Fare>();

        // Simple helper
        public bool IsMaintenanceOverdue => (DateTime.UtcNow - LastServiceDate).TotalDays > 90;
    }
}
