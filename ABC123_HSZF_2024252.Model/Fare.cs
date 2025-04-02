using System;

namespace ABC123_HSZF_2024252.Model
{
    public class Fare
    {
        public int Id { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public double Distance { get; set; }
        public decimal PaidAmount { get; set; }
        public DateTime FareStartDate { get; set; } = DateTime.UtcNow;

        // Foreign key
        public int TaxiCarId { get; set; }
        public virtual TaxiCar Car { get; set; }

        // Example helper
        public bool IsLongTrip => Distance > 100;
    }
}
