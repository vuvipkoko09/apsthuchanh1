using System.ComponentModel.DataAnnotations;

namespace ConnectDB.Models 
{
    public class User : AuditableEntity
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Username { get; set; }

        [Required]
        [MaxLength(255)]
        public string PasswordHash { get; set; }

        [Required]
        [MaxLength(100)]
        public string FullName { get; set; }

        [Required]
        [MaxLength(100)]
        public string Email { get; set; }

        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        [MaxLength(50)]
        public string? FirstName { get; set; }

        [MaxLength(50)]
        public string? LastName { get; set; }

        [MaxLength(500)]
        public string? Avatar { get; set; }

        [MaxLength(255)]
        public string? Address { get; set; }

        [MaxLength(10)]
        public string? Gender { get; set; }

        public DateTime? Birthday { get; set; }

        [MaxLength(20)]
        public string Status { get; set; } = "Active";

        [MaxLength(20)]
        public string Role { get; set; } // Ví dụ: "Admin" hoặc "Staff"

        [MaxLength(255)]
        public string? PasswordResetToken { get; set; }

        public DateTime? ResetTokenExpiry { get; set; }

        // Navigation Property: 1 User có thể tạo nhiều Phiếu giao dịch
        public virtual ICollection<InventoryTransaction>? InventoryTransactions { get; set; }
    }
}