using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChesterMeatMatrix.Models
{
    public class BatchItemQrCode
    {
        [Key]
        public int BatchItemQrCodeId { get; set; }

        public int BatchId { get; set; }

        [ForeignKey("BatchId")]
        public virtual StockBatch StockBatch { get; set; }

        [Required]
        [StringLength(100)]
        public string ItemCode { get; set; }

        public string QrCodeData { get; set; }

        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }

        public decimal UnitWeightKg { get; set; }

        public DateTime CreatedDate { get; set; }

        public BatchItemQrCode()
        {
            CreatedDate = DateTime.UtcNow;
        }
    }
}