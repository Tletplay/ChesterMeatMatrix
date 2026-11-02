using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChesterMeatMatrix.Models
{
    public enum AssignmentStatus
    {
        Pending,
        Accepted,
        Rejected,
        Completed
    }

    public class TaskAssignment
    {
        [Key]
        public int AssignmentId { get; set; }

        public int WorkTaskId { get; set; }
        [ForeignKey("WorkTaskId")]
        public virtual WorkTask WorkTask { get; set; }

        public int StaffId { get; set; }
        [ForeignKey("StaffId")]
        public virtual User Staff { get; set; }

        public int AssignedById { get; set; }
        [ForeignKey("AssignedById")]
        public virtual User AssignedBy { get; set; }

        public DateTime AssignedDate { get; set; }
        public DateTime? AcknowledgedDate { get; set; }
        public DateTime? CompletedDate { get; set; }

        public AssignmentStatus Status { get; set; }

        [StringLength(500)]
        public string Notes { get; set; }

        [StringLength(200)]
        public string AcknowledgmentNotes { get; set; }

        public TaskAssignment()
        {
            AssignedDate = DateTime.Now;
            Status = AssignmentStatus.Pending;
        }
    }
}