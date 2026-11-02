using System;
using System.ComponentModel.DataAnnotations;

namespace ChesterMeatMatrix.Models
{
    public class StockAdjustment
    {
        [Key]
        public int StockAdjustmentId { get; set; }

        public int ProductId { get; set; }
        public virtual Product Product { get; set; }

        [Display(Name = "Adjustment Type")]
        [StringLength(50)]
        public string AdjustmentType { get; set; }

        [Display(Name = "Quantity Change")]
        public int QuantityChange { get; set; }

        [Display(Name = "Reason")]
        [StringLength(500)]
        public string Reason { get; set; }

        [Display(Name = "Reference")]
        [StringLength(100)]
        public string Reference { get; set; }

        [Display(Name = "Date")]
        public DateTime AdjustmentDate { get; set; } = DateTime.Now;

        [Display(Name = "Performed By")]
        [StringLength(100)]
        public string PerformedBy { get; set; }
    }
}