using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChesterMeatMatrix.Models
{
    public class EnterpriseCartItem
    {
        [Key]
        public int CartItemId { get; set; }

        public int CartId { get; set; }

        [ForeignKey("CartId")]
        public virtual EnterpriseCart Cart { get; set; }

        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }

        [StringLength(200)]
        public string ProductName { get; set; }

        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal WeightPerUnitKg { get; set; }
    }
}