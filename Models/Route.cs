using System;
using System.Collections.Generic;

namespace ChesterMeatMatrix.Models
{
    public class Route
    {
        public int Id { get; set; }
        public string Name { get; set; }

        // Destination kept for display but OrderIds will link to actual customer addresses
        public string Destination { get; set; }

        public DateTime Date { get; set; }
        public int ItemsCount { get; set; }
        public decimal TotalMassKg { get; set; }
        public List<int> OrderIds { get; set; } = new List<int>();
        public int? AssignedDriverId { get; set; }
        public int? AssignedVehicleId { get; set; }

        // e.g., Pending, Assigned, InProgress, Done
        public string Status { get; set; }
        // Add these to your existing Route class
        public int OrderCount { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime? SignatureDate { get; set; }
        public string SignatureData { get; set; }

        public string DeliveryPhotoUrl { get; set; }  // Stores Base64 of delivery proof photo
    }
}
