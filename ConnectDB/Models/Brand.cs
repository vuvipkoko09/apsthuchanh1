using System.ComponentModel.DataAnnotations;

namespace ConnectDB.Models
{
    public class Brand : AuditableEntity // Kế thừa log
    {
        public Brand()
        {
            Products = new HashSet<Product>();
        }

        [Key]
        public int BrandId { get; set; }

        [Required]
        [MaxLength(100)]
        public string BrandName { get; set; }

        [MaxLength(100)]
        public string Origin { get; set; }

        public string? ImageUrl { get; set; }

        [MaxLength(255)]
        public string? Website { get; set; }

        [MaxLength(255)]
        public string? ContactInfo { get; set; }

        [MaxLength(20)]
        public string Status { get; set; } = "Active";

        public virtual ICollection<Product>? Products { get; set; }
    }
}