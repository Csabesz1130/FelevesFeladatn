#!/usr/bin/env bash

# create-model-files.sh
# =========================================
# Creates the "ABC123_HSZF_2024252.Model" directory and generates
# TaxiCar.cs and Fare.cs with sample content.

echo "Creating model folder if it does not exist..."
mkdir -p ABC123_HSZF_2024252.Model

echo "Generating TaxiCar.cs..."
cat <<EOF > ABC123_HSZF_2024252.Model/TaxiCar.cs
using System;
using System.Collections.Generic;

namespace ABC123_HSZF_2024252.Model
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
EOF

echo "Generating Fare.cs..."
cat <<EOF > ABC123_HSZF_2024252.Model/Fare.cs
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
EOF

echo "Model files have been created!"
