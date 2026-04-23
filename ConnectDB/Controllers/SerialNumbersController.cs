using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ConnectDB.Models;
using ConnectDB.Data;

namespace ConnectDB.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SerialNumbersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SerialNumbersController(AppDbContext context)
        {
            _context = context;
        }

        // Lấy tất cả IMEI đang có trong hệ thống
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SerialNumber>>> GetAllSerials()
        {
            return await _context.SerialNumbers
                .Include(s => s.Product)
                .ToListAsync();
        }

        // Tra cứu vết của 1 mã IMEI bất kỳ (Lịch sử của máy)
        [HttpGet("track/{imei}")]
        public async Task<ActionResult<SerialNumber>> TrackImei(string imei)
        {
            var serial = await _context.SerialNumbers
                .Include(s => s.Product)
                .Include(s => s.InboundTransaction)  // Kéo thông tin xe lúc nhập
                .Include(s => s.OutboundTransaction) // Kéo thông tin phiếu lúc xuất
                .FirstOrDefaultAsync(s => s.ImeiCode == imei);

            if (serial == null) return NotFound(new { message = "Mã IMEI này không tồn tại trong hệ thống!" });

            return serial;
        }

        // Xem báo cáo: Danh sách các máy đang bị HƯ HỎNG (DAMAGED)
        [HttpGet("damaged")]
        public async Task<ActionResult<IEnumerable<SerialNumber>>> GetDamagedItems()
        {
            return await _context.SerialNumbers
                .Include(s => s.Product)
                .Include(s => s.InboundTransaction) // Để biết xe nào làm hỏng mà đền bù
                .Where(s => s.Status == "DAMAGED")
                .ToListAsync();
        }

        // Lấy các SerialNumber đang trong kho của 1 sản phẩm cụ thể
        [HttpGet("product/{productId}")]
        public async Task<ActionResult<IEnumerable<SerialNumber>>> GetSerialsByProduct(int productId)
        {
            return await _context.SerialNumbers
                .Where(s => s.ProductId == productId && s.Status == "IN_STOCK")
                .ToListAsync();
        }
    }
}