using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChesterMeatMatrix.Models
{
    public enum StockCountStatus
    {
        Pending,
        Counted,
        Verified,
        Variance
    }

    public class StockCountItem
    {
        [Key]
        public int StockCountItemId { get; set; }

        public int StockTakeId { get; set; }
        [ForeignKey("StockTakeId")]
        public virtual StockTake StockTake { get; set; }

        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }

        [Display(Name = "System Quantity")]
        public int SystemQuantity { get; set; }

        [Display(Name = "Physical Count")]
        public int? PhysicalCount { get; set; }

        [Display(Name = "Variance")]
        public int Variance { get; set; }

        [Display(Name = "Counted By")]
        public int? CountedByStaffId { get; set; }
        [ForeignKey("CountedByStaffId")]
        public virtual User CountedByStaff { get; set; }

        [Display(Name = "Count Status")]
        public StockCountStatus Status { get; set; }

        [Display(Name = "Counted Date")]
        public DateTime? CountedDate { get; set; }

        [Display(Name = "Verified By")]
        public int? VerifiedByStaffId { get; set; }
        [ForeignKey("VerifiedByStaffId")]
        public virtual User VerifiedByStaff { get; set; }

        [Display(Name = "Verified Date")]
        public DateTime? VerifiedDate { get; set; }

        [Display(Name = "Notes")]
        [StringLength(500)]
        public string Notes { get; set; }

        public StockCountItem()
        {
            Status = StockCountStatus.Pending;
        }
    }
}