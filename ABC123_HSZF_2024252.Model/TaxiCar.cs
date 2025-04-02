namespace CarSharingHub_ABC123_HSZF_2024252.Model
{
    public class TaxiCar
    {
        public int Id { get; set; }
        public string LicensePlate { get; set; }
        public string Driver { get; set; }

        // New property
        public string VehicleType { get; set; } = "Standard";

        // Another new property
        public DateTime LastServiceDate { get; set; } = DateTime.UtcNow;

        // Example computed property or method
        public bool IsMaintenanceNeeded => (DateTime.UtcNow - LastServiceDate).TotalDays > 90;

        public virtual ICollection<Fare> Fares { get; set; } = new List<Fare>();
    }
}
