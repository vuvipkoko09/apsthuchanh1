using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ConnectDB.Models
{
    public class Shipment : AuditableEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TransactionId { get; set; }
        [ForeignKey("TransactionId")]
        public virtual InventoryTransaction? Transaction { get; set; }

        [Required]
        [MaxLength(100)]
        public string CarrierName { get; set; }

        [MaxLength(100)]
        public string? TrackingNumber { get; set; }

        [MaxLength(20)]
        public string Status { get; set; } = "Pending"; // Pending, Shipped, Delivered

        public DateTime? HandoverTime { get; set; }
        
        [MaxLength(50)]
        public string? DriverName { get; set; }
        [MaxLength(20)]
        public string? DriverPhone { get; set; }

        [MaxLength(100)]
        public string? RecipientName { get; set; }

        [MaxLength(20)]
        public string? RecipientPhone { get; set; }

        [MaxLength(255)]
        public string? DeliveryAddress { get; set; }
    }
}
