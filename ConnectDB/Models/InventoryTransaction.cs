using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ConnectDB.Models
{
    public class InventoryTransaction : AuditableEntity // Kế thừa log
    {
        [Key]
        public int TransactionId { get; set; }

        [Required]
        [MaxLength(20)]
        public string Type { get; set; } // "INBOUND" hoặc "OUTBOUND"

        // Đã bỏ CreatedDate cũ vì lớp cha đã có CreatedAt
        public DateTime? ActualTime { get; set; }

        [MaxLength(200)]
        public string? TransportInfo { get; set; }

        [MaxLength(500)]
        public string? Note { get; set; }

        [MaxLength(100)]
        public string? ReferenceNumber { get; set; } // Số hóa đơn, số lệnh xuất/nhập

        [MaxLength(20)]
        public string Status { get; set; } = "Completed"; // Planned, InProgress, Completed, Cancelled

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; } // Tổng giá trị phiếu nhập/xuất

        [Required]
        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
}