using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChesterMeatMatrix.Models
{
    public enum StockTakeAssignmentStatus
    {
        Pending,
        InProgress,
        Completed
    }

    public class StockTakeAssignment
    {
        [Key]
        public int StockTakeAssignmentId { get; set; }

        public int StockTakeId { get; set; }
        [ForeignKey("StockTakeId")]
        public virtual StockTake StockTake { get; set; }

        public int StaffId { get; set; }
        [ForeignKey("StaffId")]
        public virtual User Staff { get; set; }

        public int AssignedById { get; set; }
        [ForeignKey("AssignedById")]
        public virtual User AssignedBy { get; set; }

        [Display(Name = "Assigned Date")]
        public DateTime AssignedDate { get; set; }

        [Display(Name = "Status")]
        public StockTakeAssignmentStatus Status { get; set; }

        [Display(Name = "Location/Section")]
        [StringLength(200)]
        public string AssignedLocation { get; set; }

        [Display(Name = "Notes")]
        [StringLength(500)]
        public string Notes { get; set; }

        [Display(Name = "Items Counted")]
        public int ItemsCounted { get; set; }

        [Display(Name = "Completed Date")]
        public DateTime? CompletedDate { get; set; }

        public StockTakeAssignment()
        {
            AssignedDate = DateTime.Now;
            Status = StockTakeAssignmentStatus.Pending;
        }
    }
}