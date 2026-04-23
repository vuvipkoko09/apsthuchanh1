using System.ComponentModel.DataAnnotations;

namespace ConnectDB.Models
{
    public class AuditLog
    {
        [Key]
        public int Id { get; set; }

        public string? UserId { get; set; }

        public string? UserName { get; set; }

        [Required]
        [MaxLength(20)]
        public string Action { get; set; } // "CREATE", "UPDATE", "DELETE"

        [Required]
        [MaxLength(100)]
        public string EntityName { get; set; } // "Product", "Brand", "Category"

        [Required]
        [MaxLength(50)]
        public string EntityId { get; set; }

        public string? Changes { get; set; } // JSON or string describing changes

        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
