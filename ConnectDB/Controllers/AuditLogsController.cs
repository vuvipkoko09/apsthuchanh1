using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ConnectDB.Models;
using ConnectDB.Data;
using Microsoft.AspNetCore.Authorization;

namespace ConnectDB.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")] // Chỉ Admin mới được xem nhật ký hệ thống
    public class AuditLogsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AuditLogsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/AuditLogs
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AuditLog>>> GetAuditLogs()
        {
            return await _context.AuditLogs
                .OrderByDescending(l => l.Timestamp)
                .Take(500) // Giới hạn 500 bản ghi gần nhất
                .ToListAsync();
        }

        // GET: api/AuditLogs/5
        [HttpGet("{id}")]
        public async Task<ActionResult<AuditLog>> GetAuditLog(int id)
        {
            var auditLog = await _context.AuditLogs.FindAsync(id);

            if (auditLog == null) return NotFound();

            return auditLog;
        }

        // DELETE: api/AuditLogs/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAuditLog(int id)
        {
            var auditLog = await _context.AuditLogs.FindAsync(id);
            if (auditLog == null) return NotFound();

            _context.AuditLogs.Remove(auditLog);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã xóa bản ghi nhật ký!" });
        }

        // DELETE: api/AuditLogs/clear
        [HttpDelete("clear")]
        public async Task<IActionResult> ClearAuditLogs()
        {
            _context.AuditLogs.RemoveRange(_context.AuditLogs);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã dọn dẹp toàn bộ nhật ký hệ thống!" });
        }
    }
}
