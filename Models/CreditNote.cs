using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChesterMeatMatrix.Models
{
    public class CreditNote
    {
        [Key]
        public int CreditNoteId { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Credit Note Number")]
        public string CreditNoteNumber { get; set; }

        public int ReturnId { get; set; }

        [ForeignKey("ReturnId")]
        public virtual Return Return { get; set; }

        [Display(Name = "Amount")]
        [DataType(DataType.Currency)]
        public decimal Amount { get; set; }

        [Display(Name = "Remaining Balance")]
        [DataType(DataType.Currency)]
        public decimal RemainingBalance { get; set; }

        [Display(Name = "Amount Used")]
        [DataType(DataType.Currency)]
        public decimal AmountUsed { get; set; }

        [StringLength(20)]
        [Display(Name = "Status")]
        public string Status { get; set; }

        [Display(Name = "Issue Date")]
        [DataType(DataType.Date)]
        public DateTime IssueDate { get; set; }

        [Display(Name = "Expiry Date")]
        [DataType(DataType.Date)]
        public DateTime? ExpiryDate { get; set; }

        [Display(Name = "Issued By")]
        [StringLength(100)]
        public string IssuedBy { get; set; }

        [StringLength(500)]
        [Display(Name = "Notes")]
        public string Notes { get; set; }

        public int CustomerId { get; set; }

        [ForeignKey("CustomerId")]
        public virtual Customer Customer { get; set; }

        public CreditNote()
        {
            IssueDate = DateTime.Now;
            Status = "Active";
            Amount = 0;
            RemainingBalance = 0;
            AmountUsed = 0;
            CreditNoteNumber = string.Format("CN-{0:yyyyMMdd}-{1}", DateTime.Now, new Random().Next(1000, 9999));
        }
    }
}