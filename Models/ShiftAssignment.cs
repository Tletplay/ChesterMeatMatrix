using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChesterMeatMatrix.Models
{
    public enum ShiftAssignmentStatus
    {
        Pending,
        Confirmed,
        Completed,
        Absent
    }

    public class ShiftAssignment
    {
        [Key]
        public int ShiftAssignmentId { get; set; }

        public int ShiftId { get; set; }
        [ForeignKey("ShiftId")]
        public virtual Shift Shift { get; set; }

        public int StaffId { get; set; }
        [ForeignKey("StaffId")]
        public virtual User Staff { get; set; }

        public int AssignedById { get; set; }
        [ForeignKey("AssignedById")]
        public virtual User AssignedBy { get; set; }

        [Display(Name = "Assignment Date")]
        public DateTime AssignedDate { get; set; }

        [Display(Name = "Status")]
        public ShiftAssignmentStatus Status { get; set; }

        [Display(Name = "Notes")]
        [StringLength(500)]
        public string Notes { get; set; }

        [Display(Name = "Check In Time")]
        public DateTime? CheckInTime { get; set; }

        [Display(Name = "Check Out Time")]
        public DateTime? CheckOutTime { get; set; }

        public ShiftAssignment()
        {
            AssignedDate = DateTime.Now;
            Status = ShiftAssignmentStatus.Pending;
        }
    }
}