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
    public class DamageReportsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DamageReportsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetDamageReports()
        {
            var reports = await _context.DamageReports
                .Include(r => r.Product)
                .Include(r => r.SerialNumber)
                .Include(r => r.Reporter)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new {
                    r.Id,
                    ProductName = r.Product.Name,
                    ProductSku = r.Product.SKU,
                    Imei = r.SerialNumber != null ? r.SerialNumber.ImeiCode : null,
                    ReporterName = r.Reporter.FullName,
                    r.Status,
                    r.Note,
                    r.Quantity,
                    r.CreatedAt
                })
                .ToListAsync();

            return reports;
        }

        [HttpPost]
        public async Task<ActionResult<DamageReport>> CreateDamageReport([FromBody] DamageReportDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

            var report = new DamageReport
            {
                ProductId = dto.ProductId,
                SerialNumberId = dto.SerialNumberId,
                TargetTransactionId = dto.TargetTransactionId,
                ReporterUserId = userId,
                Note = dto.Note,
                Quantity = dto.Quantity,
                DamageType = dto.DamageType,
                Status = "Pending"
            };

            _context.DamageReports.Add(report);

            // Tự động chuyển IMEI thành "Hỏng" / "Lỗi" nếu có Serial Number
            if (dto.SerialNumberId.HasValue)
            {
                var sn = await _context.SerialNumbers.FindAsync(dto.SerialNumberId.Value);
                if (sn != null)
                {
                    sn.Status = "Defective";
                    _context.SerialNumbers.Update(sn);
                }
            }

            await _context.SaveChangesAsync();

            await LogAction("CREATE", report.Id.ToString(), $"Báo cáo hàng lỗi: {report.Note} (Số lượng: {report.Quantity})");

            return CreatedAtAction(nameof(GetDamageReports), new { id = report.Id }, report);
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateDamageStatusDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var report = await _context.DamageReports.FindAsync(id);
            if (report == null) return NotFound();

            report.Status = dto.Status;
            if (!string.IsNullOrEmpty(dto.Resolution))
            {
                report.Resolution = dto.Resolution;
            }
            
            // Xử lý logic nghiệp vụ nếu "Approved"
            if (dto.Status == "Approved")
            {
                if (report.SerialNumberId.HasValue)
                {
                    var sn = await _context.SerialNumbers.FindAsync(report.SerialNumberId.Value);
                    if (sn != null)
                    {
                        sn.Status = "DAMAGED";
                        sn.ConditionNote = $"Phát hiện lỗi: {report.Note}";
                        _context.SerialNumbers.Update(sn);
                    }
                }
                else
                {
                    // Logic cho hàng không theo Serial (ví dụ: phụ kiện)
                    // Hàng hỏng coi như xuất kho huỷ (Write-off)
                    // Ở đây có thể tạo 1 InventoryTransaction loại "ADJUSTMENT" hoặc "WRITE_OFF" nếu cần tracking tích cực hơn.
                }
            }

            await _context.SaveChangesAsync();

            await LogAction("UPDATE_STATUS", id.ToString(), $"Cập nhật trạng thái báo cáo lỗi: {report.Status}");

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
                EntityName = "DamageReport",
                EntityId = entityId,
                Changes = changes,
                Timestamp = DateTime.Now
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }

    public class DamageReportDto
    {
        [Required]
        public int ProductId { get; set; }
        public int? SerialNumberId { get; set; }
        public int? TargetTransactionId { get; set; }

        [Required]
        [MinLength(10, ErrorMessage = "Vui lòng mô tả chi tiết lỗi ít nhất 10 ký tự")]
        public string Note { get; set; }

        [Range(1, 10000, ErrorMessage = "Số lượng lỗi phải từ 1 đến 10,000")]
        public int Quantity { get; set; } = 1;
        public string? DamageType { get; set; }
    }

    public class UpdateDamageStatusDto
    {
        [Required]
        public string Status { get; set; }
        public string? Resolution { get; set; }
    }
}
