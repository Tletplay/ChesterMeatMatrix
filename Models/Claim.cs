using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChesterMeatMatrix.Models
{
    public class Claim
    {
        [Key]
        public int ClaimId { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Claim Number")]
        public string ClaimNumber { get; set; }

        public int OrderId { get; set; }

        [ForeignKey("OrderId")]
        public virtual Order Order { get; set; }

        public int CustomerId { get; set; }

        [ForeignKey("CustomerId")]
        public virtual Customer Customer { get; set; }

        public int? ProductId { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Claim Type")]
        public string ClaimType { get; set; } // "Spoiled", "Wrong Product", "Quantity Mismatch", "Weight Issue", "Damaged"

        [Required]
        [StringLength(500)]
        [Display(Name = "Description")]
        public string Description { get; set; }

        [StringLength(200)]
        [Display(Name = "Photo URL")]
        public string PhotoUrl { get; set; }

        [StringLength(50)]
        [Display(Name = "Status")]
        public string Status { get; set; } // "Pending", "Approved", "Rejected", "Credited"

        [Display(Name = "Claim Date")]
        public DateTime ClaimDate { get; set; }

        [Display(Name = "Reviewed Date")]
        public DateTime? ReviewedDate { get; set; }

        [StringLength(100)]
        [Display(Name = "Reviewed By")]
        public string ReviewedBy { get; set; }

        [StringLength(500)]
        [Display(Name = "Resolution Notes")]
        public string ResolutionNotes { get; set; }

        [Display(Name = "Credit Amount")]
        [DataType(DataType.Currency)]
        public decimal CreditAmount { get; set; }

        public Claim()
        {
            ClaimDate = DateTime.Now;
            Status = "Pending";
        }
    }
}