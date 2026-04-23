using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ConnectDB.Models
{
    public class SerialNumber : AuditableEntity // Kế thừa log
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
        public string? ConditionNote { get; set; }

        [MaxLength(50)]
        public string? WarehouseLocation { get; set; } // Vị trí cụ thể trong kho (Kệ A-1, B-2)

        [MaxLength(50)]
        public string? Color { get; set; }

        [Required]
        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }

        public int? InboundTransactionId { get; set; }
        [ForeignKey("InboundTransactionId")]
        public virtual InventoryTransaction? InboundTransaction { get; set; }

        public int? OutboundTransactionId { get; set; }
        [ForeignKey("OutboundTransactionId")]
        public virtual InventoryTransaction? OutboundTransaction { get; set; }
    }
}