using System.ComponentModel.DataAnnotations;

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
        public string TransportInfo { get; set; } // Biển số xe, tài xế

        [MaxLength(500)]
        public string Note { get; set; }
    }
}
