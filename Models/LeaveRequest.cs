using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChesterMeatMatrix.Models
{
    public enum LeaveType
    {
        Annual,
        Sick,
        Personal,
        Maternity,
        Paternity,
        Bereavement,
        Study,
        Unpaid
    }

    public enum LeaveStatus
    {
        Pending,
        Approved,
        Rejected,
        Cancelled
    }

    public class LeaveRequest
    {
        [Key]
        public int LeaveRequestId { get; set; }

        public int StaffId { get; set; }
        [ForeignKey("StaffId")]
        public virtual User Staff { get; set; }

        [Required]
        [Display(Name = "Leave Type")]
        public LeaveType Type { get; set; }

        [Required]
        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required]
        [Display(Name = "End Date")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [Display(Name = "Total Days")]
        public int TotalDays { get; set; }

        [Display(Name = "Status")]
        public LeaveStatus Status { get; set; }

        [Display(Name = "Request Date")]
        public DateTime RequestDate { get; set; }

        [Display(Name = "Approved/Rejected Date")]
        public DateTime? DecisionDate { get; set; }

        [StringLength(100)]
        [Display(Name = "Reviewed By")]
        public string ReviewedBy { get; set; }

        [StringLength(500)]
        [Display(Name = "Reason")]
        public string Reason { get; set; }

        [StringLength(500)]
        [Display(Name = "Manager Notes")]
        public string ManagerNotes { get; set; }

        public LeaveRequest()
        {
            RequestDate = DateTime.Now;
            Status = LeaveStatus.Pending;
            TotalDays = 1;
        }
    }
}