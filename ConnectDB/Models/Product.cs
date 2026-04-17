using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ConnectDB.Models
{
    public class Product
    {
        [Key]
        public int ProductId { get; set; }

        [Required]
        [MaxLength(50)]
        public string SKU { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        public string? ImageUrl { get; set; }

        // Khóa ngoại liên kết tới Category
        [Required]
        public int CategoryId { get; set; }
        [ForeignKey("CategoryId")]
        public virtual Category? Category { get; set; }

        // Khóa ngoại liên kết tới Brand
        [Required]
        public int BrandId { get; set; }
        [ForeignKey("BrandId")]
        public virtual Brand? Brand { get; set; }

        // Navigation Property: 1 Product có nhiều mã IMEI
        public virtual ICollection<SerialNumber>? SerialNumbers { get; set; }
    }
}
