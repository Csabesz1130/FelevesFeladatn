using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace BRK0Y5_HSZF_2024252.Model
{
    public class Customer
    {
        public int Id { get; set; }
        
        [XmlElement("n")] // Using 'n' tag from XML example
        public string Name { get; set; }
        
        [XmlElement("Balance")]
        public decimal Balance { get; set; }

        // Navigation property
        [XmlIgnore]
        public virtual ICollection<Fare> Fares { get; set; } = new List<Fare>();

        // Sufficient funds check property (minimum 40€)
        [XmlIgnore]
        public bool HasSufficientFunds => Balance >= 40m;
    }
}