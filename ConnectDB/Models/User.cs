using System.ComponentModel.DataAnnotations;

namespace ConnectDB.Models 
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Username { get; set; }

        [Required]
        [MaxLength(255)]
        public string PasswordHash { get; set; } // Thực tế phải mã hóa, không lưu password trơn nhé

        [Required]
        [MaxLength(100)]
        public string FullName { get; set; }

        [Required]
        [MaxLength(20)]
        public string Role { get; set; } // Ví dụ: "Admin" hoặc "Staff"

        // Navigation Property: 1 User có thể tạo nhiều Phiếu giao dịch
        public virtual ICollection<InventoryTransaction>? InventoryTransactions { get; set; }
    }
}