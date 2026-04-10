using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ConnectDB.Models
{
    public class SerialNumber
    {
        [Key]
        public int SerialId { get; set; }

        [Required]
        [MaxLength(100)]
        public string ImeiCode { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } // IN_STOCK, SOLD, DAMAGED

        [MaxLength(500)]
        public string? ConditionNote { get; set; } // Ghi chú tình trạng nếu hư hỏng

        // Khóa ngoại liên kết tới Product
        [Required]
        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }

        // Khóa ngoại lưu vết Nhập kho
        public int? InboundTransactionId { get; set; }
        [ForeignKey("InboundTransactionId")]
        public virtual InventoryTransaction? InboundTransaction { get; set; }

        // Khóa ngoại lưu vết Xuất kho (Cho phép Null vì lúc mới nhập chưa xuất)
        public int? OutboundTransactionId { get; set; }
        [ForeignKey("OutboundTransactionId")]
        public virtual InventoryTransaction? OutboundTransaction { get; set; }
    }
}
