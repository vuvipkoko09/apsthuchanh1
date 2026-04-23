using ConnectDB.Data;
using ConnectDB.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ConnectDB.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TrackingController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TrackingController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("imei/{imeiCode}")]
        public async Task<IActionResult> GetImeiLifecycle(string imeiCode)
        {
            try
            {
                var serial = await _context.SerialNumbers
                    .Include(s => s.Product)
                    .Include(s => s.InboundTransaction).ThenInclude(t => t.User)
                    .Include(s => s.OutboundTransaction).ThenInclude(t => t.User)
                    .FirstOrDefaultAsync(s => s.ImeiCode == imeiCode);

                if (serial == null)
                    return NotFound(new { message = "Không tìm thấy mã IMEI này trong hệ thống!" });

                var timeline = new List<object>();

                // 1. Sự kiện Nhập kho
                if (serial.InboundTransaction != null)
                {
                    timeline.Add(new
                    {
                        title = "Inbound (Nhập kho)",
                        date = serial.InboundTransaction.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                        description = $"Performed by: {serial.InboundTransaction.User?.FullName ?? "System"}",
                        status = "success"
                    });
                }

                // 3. Sự kiện trạng thái (Ví dụ: Kiểm định - mặc định sau nhập kho)
                timeline.Add(new
                {
                    title = "System Logging",
                    date = serial.InboundTransaction?.CreatedAt.AddHours(1).ToString("yyyy-MM-dd HH:mm") ?? DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                    description = $"Status identified as: {serial.Status}",
                    status = serial.Status == "DAMAGED" ? "error" : "primary"
                });

                // 4. Sự kiện Hư hỏng (Nếu có)
                if (serial.Status == "DAMAGED")
                {
                    timeline.Add(new
                    {
                        title = "Damage Reported",
                        date = serial.UpdatedAt?.ToString("yyyy-MM-dd HH:mm") ?? DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                        description = $"Item marked as defective: {serial.ConditionNote ?? "No details provided"}",
                        status = "error"
                    });
                }

                // 5. Sự kiện Xuất kho
                if (serial.OutboundTransaction != null)
                {
                    timeline.Add(new
                    {
                        title = "Outbound/Sold (Xuất kho)",
                        date = serial.OutboundTransaction.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                        description = $"Order ID: #{serial.OutboundTransactionId} | Performed by: {serial.OutboundTransaction.User?.FullName ?? "System"}",
                        status = serial.Status == "SOLD" ? "warning" : "success"
                    });
                }

                return Ok(new
                {
                    imei = serial.ImeiCode,
                    productName = serial.Product.Name,
                    sku = serial.Product.SKU,
                    status = serial.Status,
                    image = serial.Product.ImageUrl,
                    timeline = timeline
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống: " + ex.Message });
            }
        }
    }
}
