using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChesterMeatMatrix.Models
{
    public class Pallet
    {
        [Key]
        public int PalletId { get; set; }

        [Required]
        [StringLength(100)]
        public string PalletNumber { get; set; }

        public DateTime CreatedDate { get; set; }

        // ADD THIS PROPERTY
        [Display(Name = "Quantity")]
        public int Quantity { get; set; }

        // For grouping
        public int? PalletGroupId { get; set; }

        [ForeignKey("PalletGroupId")]
        public virtual PalletGroup PalletGroup { get; set; }
    }
}