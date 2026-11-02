using System;

namespace ChesterMeatMatrix.Models
{
    public class StockMovement
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public virtual Product Product { get; set; }
        public string MovementType { get; set; } // "IN", "OUT", "ADJUST", "RETURN"
        public int Quantity { get; set; }
        public string Reference { get; set; } // Order number, Batch number, or PO number
        public DateTime MovementDate { get; set; }
        public string PerformedBy { get; set; }
        public string Notes { get; set; }
        public int? BatchId { get; set; } // Reference to StockBatch if applicable
    }
}