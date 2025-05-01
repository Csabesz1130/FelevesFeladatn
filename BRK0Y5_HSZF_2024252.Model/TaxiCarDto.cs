using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace BRK0Y5_HSZF_2024252.Model
{
    
    
    
    public class TaxiCarDto
    {
        public string LicensePlate { get; set; }
        public string Driver { get; set; }
        public List<FareDto> Fares { get; set; } = new List<FareDto>();
        public List<FareDto> Services { get; set; } = new List<FareDto>();

        
        
        
        public IEnumerable<FareDto> GetAllFares()
        {
            return Fares.Concat(Services);
        }
    }

    
    
    
    public class FareDto
    {
        public string From { get; set; }
        public string To { get; set; }
        public double Distance { get; set; }
        public decimal PaidAmount { get; set; }
        public DateTime FareStartDate { get; set; }
    }
    
    
    
    
    [XmlRoot("CarSharing")]
    public class CarSharingDto
    {
        [XmlElement("Cars")]
        public CarCollectionDto Cars { get; set; }
        
        [XmlElement("Customers")]
        public CustomerCollectionDto Customers { get; set; }
        
        [XmlElement("Trips")]
        public TripCollectionDto Trips { get; set; }
    }
    
    public class CarCollectionDto
    {
        [XmlElement("Car")]
        public List<TaxiCar> Items { get; set; } = new List<TaxiCar>();
    }
    
    public class CustomerCollectionDto
    {
        [XmlElement("Customer")]
        public List<Customer> Items { get; set; } = new List<Customer>();
    }
    
    public class TripCollectionDto
    {
        [XmlElement("Trip")]
        public List<Fare> Items { get; set; } = new List<Fare>();
    }
}
