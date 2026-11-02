using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ChesterMeatMatrix.Models
{
    public class PalletGroup
    {
        [Key]
        public int PalletGroupId { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Group Number")]
        public string GroupNumber { get; set; }

        [Display(Name = "Group Name")]
        [StringLength(100)]
        public string GroupName { get; set; }

        [Display(Name = "Product Type")]
        [StringLength(100)]
        public string ProductType { get; set; }

        [Display(Name = "Total Quantity")]
        public int TotalQuantity { get; set; }

        [Display(Name = "Location")]
        [StringLength(100)]
        public string Location { get; set; }

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; }

        [Display(Name = "Created By")]
        [StringLength(100)]
        public string CreatedBy { get; set; }

        [Display(Name = "Status")]
        [StringLength(50)]
        public string Status { get; set; } // "Active", "Dispatched", "Archived"

        public virtual ICollection<Pallet> Pallets { get; set; }

        public PalletGroup()
        {
            CreatedDate = DateTime.Now;
            Status = "Active";
            Pallets = new List<Pallet>();
        }
    }
}