using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChesterMeatMatrix.Models
{
    public enum WorkTaskType
    {
        OrderPicking,
        StockReceiving,
        Delivery,
        StockCount,
        QualityCheck
    }

    public enum WorkTaskPriority
    {
        Low,
        Medium,
        High,
        Urgent
    }

    public enum WorkTaskStatus
    {
        Pending,
        Assigned,
        InProgress,
        Completed,
        Cancelled
    }

    public class WorkTask
    {
        [Key]
        public int WorkTaskId { get; set; }

        [Required]
        [StringLength(100)]
        public string Title { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        public WorkTaskType Type { get; set; }
        public WorkTaskPriority Priority { get; set; }
        public WorkTaskStatus Status { get; set; }

        public int? ReferenceId { get; set; }
        public string ReferenceType { get; set; }

        public DateTime CreatedDate { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? CompletedDate { get; set; }

        [StringLength(100)]
        public string CreatedBy { get; set; }

        public int? AssignedToId { get; set; }
        public virtual User AssignedTo { get; set; }

        public WorkTask()
        {
            CreatedDate = DateTime.Now;
            Status = WorkTaskStatus.Pending;
            Priority = WorkTaskPriority.Medium;
        }
    }
}