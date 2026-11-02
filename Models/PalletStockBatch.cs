using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChesterMeatMatrix.Models
{
    public class PalletStockBatch
    {
        [Key]
        public int PalletStockBatchId { get; set; }

        public int PalletId { get; set; }

        [ForeignKey("PalletId")]
        public virtual Pallet Pallet { get; set; }

        public int StockBatchId { get; set; }

        [ForeignKey("StockBatchId")]
        public virtual StockBatch StockBatch { get; set; }
    }
}