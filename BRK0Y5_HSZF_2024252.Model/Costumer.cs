using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace BRK0Y5_HSZF_2024252.Model
{
    public class Customer
    {
        public int Id { get; set; }
        
        [XmlElement("n")] 
        public string Name { get; set; }
        
        [XmlElement("Balance")]
        public decimal Balance { get; set; }

        
        [XmlIgnore]
        public virtual ICollection<Fare> Fares { get; set; } = new List<Fare>();

        
        [XmlIgnore]
        public bool HasSufficientFunds => Balance >= 40m;
    }
}
