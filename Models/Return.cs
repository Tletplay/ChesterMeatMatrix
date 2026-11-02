using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChesterMeatMatrix.Models
{
    public class Return
    {
        [Key]
        public int ReturnId { get; set; }

        public int OrderId { get; set; }

        [ForeignKey("OrderId")]
        public virtual Order Order { get; set; }

        public int? DriverId { get; set; }

        [Display(Name = "Return Date")]
        public DateTime ReturnDate { get; set; }

        [Display(Name = "Status")]
        [StringLength(50)]
        public string Status { get; set; }

        [Display(Name = "Customer Reason")]
        [StringLength(500)]
        public string CustomerReason { get; set; }

        [Display(Name = "Receiver Notes")]
        [StringLength(500)]
        public string ReceiverNotes { get; set; }

        [Display(Name = "Inspection Result")]
        [StringLength(250)]
        public string InspectionResult { get; set; }

        [Display(Name = "Inspected Date")]
        public DateTime? InspectedDate { get; set; }

        [Display(Name = "Return Label Code")]
        [StringLength(50)]
        public string ReturnLabelCode { get; set; }

        [Display(Name = "Claim Record Number")]
        [StringLength(50)]
        public string ClaimRecordNumber { get; set; }

        [Display(Name = "Return Reason")]
        [StringLength(500)]
        public string ReturnReason { get; set; }

        [Display(Name = "Driver Photo URL")]
        [StringLength(500)]
        public string DriverPhotoUrl { get; set; }

        [Display(Name = "Refund Amount")]
        [DataType(DataType.Currency)]
        public decimal? RefundAmount { get; set; }

        [Display(Name = "Is Credit Issued")]
        public bool IsCreditIssued { get; set; }
        public string Condition { get; set; }
        public string StockAction { get; set; }
        public DateTime? InspectionDate { get; set; }
        

        [Display(Name = "Credit Date")]
        public DateTime? CreditDate { get; set; }

        public virtual ICollection<ReturnItem> ReturnItems { get; set; }

        public virtual ICollection<CreditNote> CreditNotes { get; set; }

        public Return()
        {
            ReturnDate = DateTime.Now;
            Status = "Pending";
            IsCreditIssued = false;
            ReturnItems = new List<ReturnItem>();
            CreditNotes = new List<CreditNote>();
        }
    }
}