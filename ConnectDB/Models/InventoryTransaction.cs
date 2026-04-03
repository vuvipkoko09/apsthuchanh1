using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ConnectDB.Models
{
    public class InventoryTransaction
    {
        [Key]
        public int TransactionId { get; set; }

        [Required]
        [MaxLength(20)]
        public string Type { get; set; } // "INBOUND" hoặc "OUTBOUND"

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? ActualTime { get; set; }

        [MaxLength(200)]
        public string TransportInfo { get; set; }

        [MaxLength(500)]
        public string Note { get; set; }

        // BỔ SUNG PHẦN NÀY: Lưu vết nhân viên tạo phiếu
        [Required]
        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
}
