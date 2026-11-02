using System;

namespace ChesterMeatMatrix.Models
{
    public class StockBatch
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public virtual Product Product { get; set; }
        public string BatchNumber { get; set; }
        public int Quantity { get; set; }
        public decimal UnitWeightKg { get; set; }
        public DateTime ReceivedDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string Location { get; set; }
        public string Supplier { get; set; }
        public decimal CostPerUnit { get; set; }
    }
}