using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ConnectDB.Models
{
    public class DamageReport : AuditableEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }

        public int? SerialNumberId { get; set; }
        [ForeignKey("SerialNumberId")]
        public virtual SerialNumber? SerialNumber { get; set; }

        [Required]
        public int ReporterUserId { get; set; }
        [ForeignKey("ReporterUserId")]
        public virtual User? Reporter { get; set; }

        public int? TargetTransactionId { get; set; } // Liên quan đến phiếu nhập nào
        [ForeignKey("TargetTransactionId")]
        public virtual InventoryTransaction? TargetTransaction { get; set; }

        [MaxLength(20)]
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected

        [MaxLength(500)]
        [MinLength(10, ErrorMessage = "Vui lòng mô tả chi tiết lỗi ít nhất 10 ký tự")]
        public string Note { get; set; }

        [Range(1, 10000, ErrorMessage = "Số lượng phải từ 1 đến 10,000")]
        public int Quantity { get; set; } = 1; // Số lượng lỗi (Nếu không có SN, áp dụng cho Product thường)
        
        [MaxLength(500)]
        public string? ImageUrl { get; set; }

        [MaxLength(100)]
        public string? DamageType { get; set; } // Physical, Software, Accessory...

        [MaxLength(500)]
        public string? Resolution { get; set; } // Đã sửa, Trả bảo hành, Thanh lý...
    }
}
