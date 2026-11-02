using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChesterMeatMatrix.Models
{
    public enum PerformanceRating
    {
        Excellent,
        Good,
        Satisfactory,
        NeedsImprovement,
        Poor
    }

    public class Performance
    {
        [Key]
        public int PerformanceId { get; set; }

        public int StaffId { get; set; }
        [ForeignKey("StaffId")]
        public virtual User Staff { get; set; }

        [Display(Name = "Review Period")]
        [StringLength(50)]
        public string ReviewPeriod { get; set; } // "Q1 2024", "January 2024", etc.

        [Display(Name = "Review Date")]
        public DateTime ReviewDate { get; set; }

        [Display(Name = "Tasks Completed")]
        public int TasksCompleted { get; set; }

        [Display(Name = "Accuracy Rate (%)")]
        public decimal AccuracyRate { get; set; }

        [Display(Name = "Efficiency (Tasks per Hour)")]
        public decimal Efficiency { get; set; }

        [Display(Name = "Punctuality Score (%)")]
        public decimal Punctuality { get; set; }

        [Display(Name = "Customer Feedback Score (%)")]
        public decimal CustomerFeedback { get; set; }

        [Display(Name = "Overall Rating")]
        public PerformanceRating OverallRating { get; set; }

        [Display(Name = "Overall Score (%)")]
        public decimal OverallScore { get; set; }

        [StringLength(500)]
        [Display(Name = "Strengths")]
        public string Strengths { get; set; }

        [StringLength(500)]
        [Display(Name = "Areas for Improvement")]
        public string AreasForImprovement { get; set; }

        [StringLength(500)]
        [Display(Name = "Manager Comments")]
        public string ManagerComments { get; set; }

        [StringLength(100)]
        [Display(Name = "Reviewed By")]
        public string ReviewedBy { get; set; }

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; }

        public Performance()
        {
            ReviewDate = DateTime.Now;
            CreatedDate = DateTime.Now;
            OverallRating = PerformanceRating.Good;
        }
    }
}