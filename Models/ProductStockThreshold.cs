using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChesterMeatMatrix.Models
{
    public class ProductStockThreshold
    {
        [Key]
        public int ProductStockThresholdId { get; set; }

        [Required]
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }

        [Display(Name = "Minimum Stock Level")]
        public int MinimumStockLevel { get; set; }

        [Display(Name = "Reorder Quantity")]
        public int ReorderQuantity { get; set; }

        [Display(Name = "Auto-Create Alert")]
        public bool AutoCreateAlert { get; set; }

        [Display(Name = "Last Updated")]
        public DateTime LastUpdated { get; set; }

        [Display(Name = "Updated By")]
        [StringLength(100)]
        public string UpdatedBy { get; set; }

        [Display(Name = "Default Batch Size")]
        public int DefaultBatchSize { get; set; }

        [Display(Name = "Default Unit Weight (kg)")]
        public decimal DefaultUnitWeightKg { get; set; }

        public ProductStockThreshold()
        {
            MinimumStockLevel = 50;
            ReorderQuantity = 100;
            AutoCreateAlert = true;
            LastUpdated = DateTime.Now;
            UpdatedBy = "System";
            DefaultBatchSize = 25;
            DefaultUnitWeightKg = 1.0m;
        }
    }
}