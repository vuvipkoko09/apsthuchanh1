using ConnectDB.Data;
using ConnectDB.Models;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ConnectDB.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class InventoryChecksController : ControllerBase
    {
        private readonly AppDbContext _context;

        public InventoryChecksController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetChecks()
        {
            return await _context.InventoryChecks
                .Include(c => c.CreatedBy)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new {
                    c.CheckId,
                    c.Name,
                    c.Status,
                    c.CompletedAt,
                    CreatedBy = c.CreatedBy.FullName,
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetCheckDetails(int id)
        {
            var check = await _context.InventoryChecks
                .Include(c => c.CreatedBy)
                .Include(c => c.Details)
                .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(c => c.CheckId == id);
            
            if (check == null) return NotFound();

            return new {
                check.CheckId,
                check.Name,
                check.Status,
                check.CompletedAt,
                CreatedBy = check.CreatedBy?.FullName,
                Details = check.Details.Select(d => new {
                    d.Id,
                    d.ProductId,
                    ProductName = d.Product?.Name,
                    ProductSku = d.Product?.SKU,
                    d.SystemQty,
                    d.ActualQty,
                    d.Discrepancy,
                    d.DiscrepancyReason,
                    d.IsResolved
                })
            };
        }

        [HttpPost]
        public async Task<ActionResult<InventoryCheck>> CreateCheck([FromBody] CreateInventoryCheckDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

            var newCheck = new InventoryCheck
            {
                Name = dto.Name,
                CreatedByUserId = userId,
                Status = "InProgress"
            };

            // Lấy lượng tồn kho hạch toán từ db (tính vo dựa trên SERIAL_NUMBERS đang ở trạng thái IN_STOCK)
            var products = await _context.Products
                .Select(p => new {
                    p.ProductId,
                    StockQty = _context.SerialNumbers.Count(s => s.ProductId == p.ProductId && s.Status == "IN_STOCK")
                })
                .ToListAsync();
            
            foreach(var p in products)
            {
               newCheck.Details.Add(new InventoryCheckDetail
               {
                   ProductId = p.ProductId,
                   SystemQty = p.StockQty,
                   ActualQty = p.StockQty, // Mặc định để bằng hệ thống, user sẽ sửa sau
                   IsResolved = false
               });
            }

            _context.InventoryChecks.Add(newCheck);
            await _context.SaveChangesAsync();

            await LogAction("CREATE", newCheck.CheckId.ToString(), $"Tạo phiên kiểm kho: {newCheck.Name}");

            return CreatedAtAction(nameof(GetCheckDetails), new { id = newCheck.CheckId }, newCheck);
        }

        [HttpPut("{checkId}/details/{detailId}")]
        public async Task<IActionResult> UpdateActualQuantity(int checkId, int detailId, [FromBody] UpdateActualQtyDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var detail = await _context.InventoryCheckDetails.FirstOrDefaultAsync(d => d.Id == detailId && d.CheckId == checkId);
            if (detail == null) return NotFound();

            detail.ActualQty = dto.ActualQty;
            detail.DiscrepancyReason = dto.Reason;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpPost("{id}/complete")]
        public async Task<IActionResult> CompleteCheck(int id)
        {
            var check = await _context.InventoryChecks.FindAsync(id);
            if (check == null) return NotFound();

            check.Status = "Completed";
            check.CompletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await LogAction("COMPLETE", id.ToString(), $"Hoàn thành phiên kiểm kho: {check.Name}");

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
                EntityName = "InventoryCheck",
                EntityId = entityId,
                Changes = changes,
                Timestamp = DateTime.Now
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }

    public class CreateInventoryCheckDto 
    { 
        [Required]
        [MinLength(5, ErrorMessage = "Tên phiên kiểm kho phải có ít nhất 5 ký tự")]
        public string Name { get; set; } 
    }

    public class UpdateActualQtyDto 
    { 
        [Range(0, 1000000, ErrorMessage = "Số lượng không thể nhỏ hơn 0")]
        public int ActualQty { get; set; } 
        
        public string? Reason { get; set; } 
    }
}
