using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ConnectDB.Models
{
    public class InventoryCheckDetail : AuditableEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CheckId { get; set; }
        [ForeignKey("CheckId")]
        public virtual InventoryCheck? InventoryCheck { get; set; }

        [Required]
        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }

        public int SystemQty { get; set; }

        [Range(0, 1000000, ErrorMessage = "Số lượng thực tế không thể nhỏ hơn 0")]
        public int ActualQty { get; set; }

        // Bằng ActualQty - SystemQty (Nếu âm thì kho thực tế thiếu, dương thì thừa)
        public int Discrepancy => ActualQty - SystemQty;

        [MaxLength(200)]
        public string? DiscrepancyReason { get; set; }

        [MaxLength(50)]
        public string? DiscrepancyAction { get; set; } // Auto-Adjust, Write-Off, Pending

        public bool IsResolved { get; set; } = false;
    }
}
