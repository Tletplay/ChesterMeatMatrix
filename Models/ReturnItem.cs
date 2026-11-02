using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChesterMeatMatrix.Models
{
    public class ReturnItem
    {
        [Key]
        public int ReturnItemId { get; set; }

        public int ReturnId { get; set; }

        [ForeignKey("ReturnId")]
        public virtual Return Return { get; set; }

        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }

        [Display(Name = "Quantity Returned")]
        public int Quantity { get; set; }

        [Display(Name = "Unit Price")]
        public decimal UnitPrice { get; set; }

        [Display(Name = "Item Condition")]
        [StringLength(50)]
        public string Condition { get; set; }

        [Display(Name = "Stock Action")]
        [StringLength(50)]
        public string StockAction { get; set; }

        [Display(Name = "Stock Adjusted")]
        public bool IsStockAdjusted { get; set; } = false;


    }
}