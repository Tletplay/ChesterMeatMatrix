using System;

namespace ChesterMeatMatrix.Models
{
    public class Product
    {
        public int Id { get; set; }

        // e.g. 0103039
        public string ProductCode { get; set; }

        // e.g. Leg Quarter
        public string Name { get; set; }

        // e.g. 9989888748 for single box
        public string SerialNumber { get; set; }

        // pallet binding flag
        public bool IsPallet { get; set; }

        // pallet identifier when grouped
        public string PalletCode { get; set; }

        // units in this record
        public int Quantity { get; set; }

        // unit price
        public decimal Price { get; set; }

        // e.g. Freezer A / Pickup Floor
        public string Location { get; set; }

        public DateTime CreatedAt { get; set; }

        // NEW: Product category for filtering (Beef, Chicken, Wors & Sausages, Lamb & Mutton, Offal, Deli & Other)
        public string Category { get; set; }

        // NEW: Product image URL (placeholder for now)
        public string ImageUrl { get; set; }
    }
}