using System;
using System.ComponentModel.DataAnnotations;

namespace ChesterMeatMatrix.Models
{
    public class Truck
    {
        [Key]
        public int TruckId { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Registration Number")]
        public string RegistrationNumber { get; set; }

        [Display(Name = "Capacity (kg)")]
        public decimal Capacity { get; set; }

        [Display(Name = "Is Available")]
        public bool IsAvailable { get; set; }

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; }
    }
}