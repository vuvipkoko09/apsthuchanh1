using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ConnectDB.Models
{
    public class InventoryCheck : AuditableEntity
    {
        [Key]
        public int CheckId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        [Required]
        public int CreatedByUserId { get; set; }
        [ForeignKey("CreatedByUserId")]
        public virtual User? CreatedBy { get; set; }

        [MaxLength(20)]
        public string Status { get; set; } = "Draft"; // Draft, InProgress, Completed, Reconciled

        public DateTime? CompletedAt { get; set; }

        public virtual ICollection<InventoryCheckDetail> Details { get; set; } = new List<InventoryCheckDetail>();
    }
}
