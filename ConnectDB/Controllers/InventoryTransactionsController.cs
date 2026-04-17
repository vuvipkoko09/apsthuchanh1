using ConnectDB.Data;
using ConnectDB.DTOs;
using ConnectDB.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ConnectDB.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InventoryTransactionsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public InventoryTransactionsController(AppDbContext context)
        {
            _context = context;
        }

        // 1. Lấy danh sách TOÀN BỘ Phiếu kho
        [HttpGet]
        public async Task<ActionResult<IEnumerable<InventoryTransaction>>> GetAllTransactions()
        {
            return await _context.InventoryTransactions
                .Include(t => t.User)
                .OrderByDescending(t => t.CreatedDate)
                .ToListAsync();
        }

        // 2. Lấy chi tiết 1 Phiếu kho (Kèm theo danh sách máy)
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetTransactionDetail(int id)
        {
            var transaction = await _context.InventoryTransactions
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.TransactionId == id);

            if (transaction == null) return NotFound(new { message = "Không tìm thấy phiếu kho!" });

            var serials = await _context.SerialNumbers
                .Where(s => s.InboundTransactionId == id || s.OutboundTransactionId == id)
                .Select(s => new { s.ImeiCode, ProductName = s.Product.Name, s.Status, s.ConditionNote })
                .ToListAsync();

            return Ok(new
            {
                TransactionInfo = transaction,
                Items = serials
            });
        }

        // 3. API NHẬP KHO (Inbound)
        [HttpPost("inbound")]
        public async Task<IActionResult> Inbound([FromBody] InboundRequest request)
        {
            if (request.Imeis == null || !request.Imeis.Any())
                return BadRequest(new { message = "Danh sách IMEI không được để trống!" });

            // Kiểm tra trùng lặp IMEI ngay trong danh sách gửi lên
            if (request.Imeis.Distinct().Count() != request.Imeis.Count)
                return BadRequest(new { message = "Có mã IMEI bị quét trùng lặp trong danh sách!" });

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Kiểm tra IMEI đã tồn tại trong DB chưa (BR1)
                var existingImeis = await _context.SerialNumbers
                    .Where(s => request.Imeis.Contains(s.ImeiCode))
                    .Select(s => s.ImeiCode)
                    .ToListAsync();

                if (existingImeis.Any())
                    return BadRequest(new { message = "Các mã IMEI sau đã tồn tại trong hệ thống!", duplicates = existingImeis });

                // Tạo Phiếu Nhập
                var newTrans = new InventoryTransaction
                {
                    UserId = request.UserId,
                    Type = "INBOUND",
                    CreatedDate = DateTime.Now,
                    ActualTime = DateTime.Now,
                    TransportInfo = request.TransportInfo,
                    Note = request.Note
                };
                _context.InventoryTransactions.Add(newTrans);
                await _context.SaveChangesAsync();

                // Tạo các bản ghi IMEI
                var serials = request.Imeis.Select(imei => new SerialNumber
                {
                    ImeiCode = imei,
                    ProductId = request.ProductId,
                    Status = "IN_STOCK",
                    InboundTransactionId = newTrans.TransactionId
                }).ToList();

                _context.SerialNumbers.AddRange(serials);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return Ok(new { message = $"Nhập kho thành công {request.Imeis.Count} máy!", transactionId = newTrans.TransactionId });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        // 4. API XUẤT KHO (Outbound)
        [HttpPost("outbound")]
        public async Task<IActionResult> Outbound([FromBody] OutboundRequest request)
        {
            if (request.Imeis == null || !request.Imeis.Any())
                return BadRequest(new { message = "Danh sách IMEI quét để xuất không được trống!" });

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Tìm máy trong kho
                var serialsInDb = await _context.SerialNumbers
                    .Where(s => request.Imeis.Contains(s.ImeiCode))
                    .ToListAsync();

                if (serialsInDb.Count != request.Imeis.Count)
                    return BadRequest(new { message = "Có mã IMEI không tồn tại trong kho!" });

                // Kiểm tra trạng thái máy (BR2 & BR4)
                var invalidSerials = serialsInDb.Where(s => s.Status != "IN_STOCK").ToList();
                if (invalidSerials.Any())
                {
                    var details = invalidSerials.Select(s => $"{s.ImeiCode} ({s.Status})");
                    return BadRequest(new { message = "Không thể xuất các máy không ở trạng thái sẵn sàng!", details });
                }

                // Tạo Phiếu Xuất
                var newTrans = new InventoryTransaction
                {
                    UserId = request.UserId,
                    Type = "OUTBOUND",
                    CreatedDate = DateTime.Now,
                    ActualTime = DateTime.Now,
                    TransportInfo = request.TransportInfo,
                    Note = request.Note
                };
                _context.InventoryTransactions.Add(newTrans);
                await _context.SaveChangesAsync();

                // Cập nhật trạng thái máy
                foreach (var serial in serialsInDb)
                {
                    serial.Status = "SOLD";
                    serial.OutboundTransactionId = newTrans.TransactionId;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { message = $"Xuất kho thành công {request.Imeis.Count} máy!", transactionId = newTrans.TransactionId });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = "Lỗi hệ thống: " + ex.Message });
            }
        }
    }
}