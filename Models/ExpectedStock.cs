using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChesterMeatMatrix.Models
{
    public class ExpectedStock
    {
        [Key]
        public int ExpectedStockId { get; set; }

        [Required]
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }

        [Required]
        [Display(Name = "Expected Quantity")]
        public int ExpectedQuantity { get; set; }

        [Display(Name = "Unit Weight (kg)")]
        public decimal ExpectedUnitWeightKg { get; set; }

        [Display(Name = "Expected Delivery Date")]
        [DataType(DataType.Date)]
        public DateTime ExpectedDeliveryDate { get; set; }

        [Display(Name = "Status")]
        [StringLength(50)]
        public string Status { get; set; }

        [Display(Name = "Supplier Reference")]
        [StringLength(100)]
        public string SupplierReference { get; set; }

        [Display(Name = "Purchase Order Number")]
        [StringLength(100)]
        public string PurchaseOrderNumber { get; set; }

        [Display(Name = "Notes")]
        [StringLength(500)]
        public string Notes { get; set; }

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; }

        [Display(Name = "Created By")]
        [StringLength(100)]
        public string CreatedBy { get; set; }

        [Display(Name = "Quantity Received")]
        public int QuantityReceived { get; set; }

        [Display(Name = "Last Updated")]
        public DateTime LastUpdated { get; set; }

        public int? LowStockAlertId { get; set; }

        [ForeignKey("LowStockAlertId")]
        public virtual LowStockAlert LowStockAlert { get; set; }

        [Display(Name = "Batch Size")]
        public int BatchSize { get; set; }

        [Display(Name = "Number of Batches")]
        public int BatchCount { get; set; }

        public ExpectedStock()
        {
            CreatedDate = DateTime.Now;
            LastUpdated = DateTime.Now;
            Status = "Pending";
            ExpectedUnitWeightKg = 1.0m;
            ExpectedQuantity = 0;
            QuantityReceived = 0;
            ExpectedDeliveryDate = DateTime.Now.AddDays(7);
            BatchSize = 25;
            BatchCount = 1;
        }
    }
}