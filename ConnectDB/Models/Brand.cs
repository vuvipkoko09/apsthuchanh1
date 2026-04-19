using System.ComponentModel.DataAnnotations;

namespace ConnectDB.Models
{
    public class Brand
    {
        // Khởi tạo sẵn danh sách để tránh lỗi Null
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

        // Đổi LogoUrl thành ImageUrl cho đồng bộ toàn hệ thống
        public string? ImageUrl { get; set; }

        // Navigation Property: 1 Brand có nhiều Product
        public virtual ICollection<Product>? Products { get; set; }
    }
}