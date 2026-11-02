using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChesterMeatMatrix.Models
{
    public enum AttendanceStatus
    {
        Present,
        Absent,
        Late,
        HalfDay,
        Leave
    }

    public class Attendance
    {
        [Key]
        public int AttendanceId { get; set; }

        public int StaffId { get; set; }
        [ForeignKey("StaffId")]
        public virtual User Staff { get; set; }

        [Display(Name = "Date")]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        [Display(Name = "Clock In")]
        public DateTime? ClockInTime { get; set; }

        [Display(Name = "Clock Out")]
        public DateTime? ClockOutTime { get; set; }

        [Display(Name = "Status")]
        public AttendanceStatus Status { get; set; }

        [Display(Name = "Total Hours")]
        public decimal TotalHours { get; set; }

        [Display(Name = "Overtime Hours")]
        public decimal OvertimeHours { get; set; }

        [Display(Name = "Break Duration (mins)")]
        public int BreakMinutes { get; set; }

        [StringLength(500)]
        [Display(Name = "Notes")]
        public string Notes { get; set; }

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; }

        public Attendance()
        {
            Date = DateTime.Today;
            CreatedDate = DateTime.Now;
            Status = AttendanceStatus.Present;
        }
    }
}