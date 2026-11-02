using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChesterMeatMatrix.Models
{
    public class LowStockAlert
    {
        [Key]
        public int LowStockAlertId { get; set; }

        [Required]
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }

        [Display(Name = "Current Stock (Units)")]
        public int CurrentStockQuantity { get; set; }

        [Display(Name = "Threshold Level")]
        public int ThresholdLevel { get; set; }

        [Display(Name = "Suggested Order Quantity")]
        public int SuggestedOrderQuantity { get; set; }

        [Display(Name = "Alert Status")]
        [StringLength(50)]
        public string AlertStatus { get; set; }

        [Display(Name = "Alert Date")]
        public DateTime AlertDate { get; set; }

        [Display(Name = "Reviewed Date")]
        public DateTime? ReviewedDate { get; set; }

        [Display(Name = "Reviewed By")]
        [StringLength(100)]
        public string ReviewedBy { get; set; }

        [Display(Name = "Reviewer Notes")]
        [StringLength(500)]
        public string ReviewerNotes { get; set; }

        [Display(Name = "Expected Delivery Date")]
        public DateTime? ExpectedDeliveryDate { get; set; }

        [Display(Name = "Supplier Reference")]
        [StringLength(100)]
        public string SupplierReference { get; set; }

        [Display(Name = "Unit Weight (kg)")]
        public decimal? ExpectedUnitWeightKg { get; set; }

        public LowStockAlert()
        {
            AlertDate = DateTime.Now;
            AlertStatus = "Pending";
            CurrentStockQuantity = 0;
            ThresholdLevel = 50;
            SuggestedOrderQuantity = 100;
        }
    }
}