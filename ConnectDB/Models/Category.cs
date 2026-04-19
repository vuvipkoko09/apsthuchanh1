using System.ComponentModel.DataAnnotations;

namespace ConnectDB.Models
{
    public class Category
    {
        // Khởi tạo sẵn danh sách để tránh lỗi Null
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

        // Thêm cột lưu link ảnh
        public string? ImageUrl { get; set; }

        // Navigation Property: 1 Category có nhiều Product
        public virtual ICollection<Product>? Products { get; set; }
    }
}