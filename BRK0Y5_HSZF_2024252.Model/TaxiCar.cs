using System;
using System.Collections.Generic;

namespace BRK0Y5_HSZF_2024252.Model
{
    public class TaxiCar
    {
        public int Id { get; set; }
        public string Model { get; set; } // Changed from LicensePlate to Model
        public string Driver { get; set; } // Kept for backward compatibility
        
        // New properties for car sharing
        public double TotalDistance { get; set; }
        public double DistanceSinceLastMaintenance { get; set; }

        // Navigation property: One car, many trips (renamed from Fares)
        public virtual ICollection<Fare> Fares { get; set; } = new List<Fare>();

        // Business logic for car sharing
        public bool NeedsMaintenance => DistanceSinceLastMaintenance >= 200.0;
        
        public bool RequestRandomMaintenance()
        {
            // 20% chance of requesting maintenance
            return new Random().NextDouble() < 0.2;
        }
        
        public void ResetMaintenanceCounter()
        {
            DistanceSinceLastMaintenance = 0;
        }
    }
}