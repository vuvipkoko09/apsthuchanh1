using System.ComponentModel.DataAnnotations;

namespace ConnectDB.Models
{
    public class Brand
    {
        [Key]
        public int BrandId { get; set; }

        [Required]
        [MaxLength(100)]
        public string BrandName { get; set; }

        [MaxLength(100)]
        public string Origin { get; set; }

        public string? LogoUrl { get; set; }

        // Navigation Property: 1 Brand có nhiều Product
        public virtual ICollection<Product>? Products { get; set; }
    }
}