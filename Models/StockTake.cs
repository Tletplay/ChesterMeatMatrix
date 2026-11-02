using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChesterMeatMatrix.Models
{
    public enum StockTakeStatus
    {
        Draft,
        InProgress,
        Counting,
        VariancesIdentified,
        Approved,
        Completed,
        Cancelled
    }

    public class StockTake
    {
        [Key]
        public int StockTakeId { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Session Name")]
        public string Name { get; set; }

        [StringLength(50)]
        [Display(Name = "Session Number")]
        public string SessionNumber { get; set; }

        [Display(Name = "Location")]
        [StringLength(200)]
        public string Location { get; set; }

        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Display(Name = "End Date")]
        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        [Display(Name = "Status")]
        public StockTakeStatus Status { get; set; }

        [Display(Name = "Created By")]
        [StringLength(100)]
        public string CreatedBy { get; set; }

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; }

        [Display(Name = "Completed By")]
        [StringLength(100)]
        public string CompletedBy { get; set; }

        [Display(Name = "Completed Date")]
        public DateTime? CompletedDate { get; set; }

        [Display(Name = "Notes")]
        [StringLength(500)]
        public string Notes { get; set; }

        [Display(Name = "Total Items Counted")]
        public int TotalItemsCounted { get; set; }

        [Display(Name = "Total Variances Found")]
        public int TotalVariancesFound { get; set; }

        public virtual ICollection<StockTakeAssignment> Assignments { get; set; }
        public virtual ICollection<StockCountItem> CountItems { get; set; }

        public StockTake()
        {
            CreatedDate = DateTime.Now;
            Status = StockTakeStatus.Draft;
            Assignments = new List<StockTakeAssignment>();
            CountItems = new List<StockCountItem>();
        }
    }
}