using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace BRK0Y5_HSZF_2024252.Model
{
    public class TaxiCar
    {
        public int Id { get; set; }
        
        [XmlElement("Model")]
        public string Model { get; set; }
        
        [XmlElement("LicensePlate")]
        public string LicensePlate { get; set; }
        
        [XmlElement("Driver")]
        public string Driver { get; set; }
        
        [XmlElement("TotalDistance")]
        public double TotalDistance { get; set; }
        
        [XmlElement("DistanceSinceLastMaintenance")]
        public double DistanceSinceLastMaintenance { get; set; }

        // Service date from original model
        public DateTime LastServiceDate { get; set; } = DateTime.UtcNow;

        // Navigation property: One car, many fares/trips
        [XmlIgnore]
        public virtual ICollection<Fare> Fares { get; set; } = new List<Fare>();

        // Maintenance check properties
        [XmlIgnore]
        public bool NeedsMaintenance => DistanceSinceLastMaintenance >= 200;
        
        [XmlIgnore]
        public bool IsMaintenanceOverdue => (DateTime.UtcNow - LastServiceDate).TotalDays > 90;
    }
}