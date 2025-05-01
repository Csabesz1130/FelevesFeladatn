using System;
using System.Xml.Serialization;

namespace BRK0Y5_HSZF_2024252.Model
{
    public class Fare
    {
        
        public const double PRICE_PER_KM = 0.35;
        public const double BASE_FEE = 0.5;

        public int Id { get; set; }
        
        [XmlElement("CarId")]
        public int CarId { get; set; }
        
        [XmlElement("CustomerId")]
        public int CustomerId { get; set; }
        
        [XmlElement("Distance")]
        public double Distance { get; set; }
        
        [XmlElement("Cost")]
        public decimal PaidAmount { get; set; }
        
        
        public string From { get; set; }
        public string To { get; set; }
        public DateTime FareStartDate { get; set; } = DateTime.UtcNow;

        
        [XmlIgnore]
        public virtual TaxiCar Car { get; set; }
        
        [XmlIgnore]
        public virtual Customer Customer { get; set; }

        
        [XmlIgnore]
        public bool IsLongTrip => Distance > 100;
        
        
        public static decimal CalculateCost(double distance)
        {
            return (decimal)(BASE_FEE + (distance * PRICE_PER_KM));
        }
    }
}
