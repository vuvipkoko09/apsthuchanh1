using System.ComponentModel.DataAnnotations;

namespace ConnectDB.Models
{
    public class Category : AuditableEntity // Kế thừa log
    {
        public Category()
        {
            Products = new HashSet<Product>();
        }

        [Key]
        public int CategoryId { get; set; }

        [Required]
        [MaxLength(100)]
        public string CategoryName { get; set; }

        [MaxLength(500)]
        public string Description { get; set; }

        public string? ImageUrl { get; set; }

        [MaxLength(20)]
        public string Status { get; set; } = "Active";

        public virtual ICollection<Product>? Products { get; set; }
    }
}