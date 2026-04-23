using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ConnectDB.Models
{
    public class Product : AuditableEntity // Kế thừa log
    {
        public Product()
        {
            SerialNumbers = new HashSet<SerialNumber>();
        }

        [Key]
        public int ProductId { get; set; }

        [Required]
        [MaxLength(50)]
        public string SKU { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        public string? ImageUrl { get; set; }

        // Đã khôi phục lại các cột giá trị thương mại
        [Column(TypeName = "decimal(18,2)")]
        public decimal CostPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SellingPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? PromotionalPrice { get; set; }

        public int WarrantyMonths { get; set; }
        
        public string? Description { get; set; }
        
        public string? Specifications { get; set; }
        
        [MaxLength(20)]
        public string Unit { get; set; } = "Unit";
        
        [MaxLength(20)]
        public string Status { get; set; } = "Active"; // Active, Discontinued, OutOfStock

        [Required]
        public int CategoryId { get; set; }
        [ForeignKey("CategoryId")]
        public virtual Category? Category { get; set; }

        [Required]
        public int BrandId { get; set; }
        [ForeignKey("BrandId")]
        public virtual Brand? Brand { get; set; }

        public virtual ICollection<SerialNumber>? SerialNumbers { get; set; }
    }
}