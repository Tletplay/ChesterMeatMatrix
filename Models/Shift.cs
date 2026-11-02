using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChesterMeatMatrix.Models
{
    public enum ShiftType
    {
        Morning,    // 06:00 - 14:00
        Afternoon,  // 14:00 - 22:00
        Night       // 22:00 - 06:00
    }

    public enum ShiftStatus
    {
        Draft,
        Published,
        Completed
    }

    public class Shift
    {
        [Key]
        public int ShiftId { get; set; }

        [Required]
        public ShiftType Type { get; set; }

        [Required]
        [Display(Name = "Shift Date")]
        [DataType(DataType.Date)]
        public DateTime ShiftDate { get; set; }

        [Display(Name = "Start Time")]
        [DataType(DataType.Time)]
        public TimeSpan StartTime { get; set; }

        [Display(Name = "End Time")]
        [DataType(DataType.Time)]
        public TimeSpan EndTime { get; set; }

        [Display(Name = "Shift Name")]
        [StringLength(100)]
        public string Name { get; set; }

        [Display(Name = "Status")]
        public ShiftStatus Status { get; set; }

        [Display(Name = "Created By")]
        [StringLength(100)]
        public string CreatedBy { get; set; }

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; }

        [Display(Name = "Published Date")]
        public DateTime? PublishedDate { get; set; }

        [Display(Name = "Notes")]
        [StringLength(500)]
        public string Notes { get; set; }

        public virtual ICollection<ShiftAssignment> ShiftAssignments { get; set; }

        public Shift()
        {
            CreatedDate = DateTime.Now;
            Status = ShiftStatus.Draft;
            ShiftAssignments = new List<ShiftAssignment>();
        }
    }
}