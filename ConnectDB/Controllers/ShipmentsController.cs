using ConnectDB.Data;
using ConnectDB.Models;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ConnectDB.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ShipmentsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ShipmentsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetShipments()
        {
            return await _context.Shipments
                .Include(s => s.Transaction)
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => new {
                    s.Id,
                    s.TransactionId,
                    TransactionNote = s.Transaction.Note,
                    s.CarrierName,
                    s.TrackingNumber,
                    s.Status,
                    s.DriverName,
                    s.DriverPhone,
                    s.HandoverTime
                })
                .ToListAsync();
        }

        [HttpPost]
        public async Task<ActionResult<Shipment>> CreateShipment([FromBody] ShipmentDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // Verify if transaction exists and is OUTBOUND
            var transaction = await _context.InventoryTransactions.FindAsync(dto.TransactionId);
            if (transaction == null || transaction.Type != "OUTBOUND")
            {
                return BadRequest(new { message = "Chỉ được bàn giao cho các phiếu xuất kho (OUTBOUND)." });
            }

            var shipment = new Shipment
            {
                TransactionId = dto.TransactionId,
                CarrierName = dto.CarrierName,
                TrackingNumber = dto.TrackingNumber,
                DriverName = dto.DriverName,
                DriverPhone = dto.DriverPhone,
                RecipientName = dto.RecipientName,
                RecipientPhone = dto.RecipientPhone,
                DeliveryAddress = dto.DeliveryAddress,
                Status = "Pending"
            };

            _context.Shipments.Add(shipment);
            await _context.SaveChangesAsync();

            await LogAction("CREATE", shipment.Id.ToString(), $"Tạo yêu cầu vận chuyển: {shipment.CarrierName} - {shipment.TrackingNumber}");

            return CreatedAtAction(nameof(GetShipments), new { id = shipment.Id }, shipment);
        }

        [HttpPut("{id}/handover")]
        public async Task<IActionResult> HandoverShipment(int id)
        {
            var shipment = await _context.Shipments.FindAsync(id);
            if (shipment == null) return NotFound();

            shipment.Status = "Shipped";
            shipment.HandoverTime = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await LogAction("HANDOVER", id.ToString(), $"Bàn giao vận chuyển cho: {shipment.CarrierName}");

            return NoContent();
        }

        private async Task LogAction(string action, string entityId, string changes)
        {
            var userId = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
            var userName = User.Identity?.Name;

            var log = new AuditLog
            {
                UserId = userId,
                UserName = userName,
                Action = action,
                EntityName = "Shipment",
                EntityId = entityId,
                Changes = changes,
                Timestamp = DateTime.Now
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }

    public class ShipmentDto
    {
        [Required]
        public int TransactionId { get; set; }

        [Required]
        [MaxLength(100)]
        public string CarrierName { get; set; }

        public string? TrackingNumber { get; set; }
        public string? DriverName { get; set; }
        
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string? DriverPhone { get; set; }

        [Required]
        public string? RecipientName { get; set; }
        
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string? RecipientPhone { get; set; }

        [Required]
        public string? DeliveryAddress { get; set; }
    }
}
