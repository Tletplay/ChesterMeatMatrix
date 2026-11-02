using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChesterMeatMatrix.Models
{
    public class SupplierReturn
    {
        [Key]
        public int SupplierReturnId { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Return Number")]
        public string ReturnNumber { get; set; }

        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }

        public int? BatchId { get; set; }

        [ForeignKey("BatchId")]
        public virtual StockBatch StockBatch { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Return Reason")]
        public string ReturnReason { get; set; } // "Defective", "Damaged", "Expired", "Wrong Item"

        [StringLength(500)]
        [Display(Name = "Notes")]
        public string Notes { get; set; }

        [StringLength(50)]
        [Display(Name = "Status")]
        public string Status { get; set; } // "Pending", "Approved", "Completed", "Rejected"

        [Display(Name = "Return Date")]
        public DateTime ReturnDate { get; set; }

        [Display(Name = "Approved Date")]
        public DateTime? ApprovedDate { get; set; }

        [StringLength(100)]
        [Display(Name = "Approved By")]
        public string ApprovedBy { get; set; }

        [StringLength(100)]
        [Display(Name = "Supplier Name")]
        public string SupplierName { get; set; }

        [StringLength(100)]
        [Display(Name = "Supplier Reference")]
        public string SupplierReference { get; set; }

        public SupplierReturn()
        {
            ReturnDate = DateTime.Now;
            Status = "Pending";
        }
    }
}