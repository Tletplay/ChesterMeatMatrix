using System.ComponentModel.DataAnnotations;

namespace ChesterMeatMatrix.Models
{
    public class StockLocation
    {
        [Key]
        public int StockLocationId { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Location Name")]
        public string Name { get; set; }

        [StringLength(100)]
        [Display(Name = "Warehouse / Zone")]
        public string Warehouse { get; set; }
    }
}