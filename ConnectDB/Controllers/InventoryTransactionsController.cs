using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ConnectDB.Models;
using ConnectDB.Data;

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

        // Lấy danh sách TOÀN BỘ Phiếu kho
        [HttpGet]
        public async Task<ActionResult<IEnumerable<InventoryTransaction>>> GetAllTransactions()
        {
            return await _context.InventoryTransactions
                .Include(t => t.User) // Hiển thị người tạo phiếu
                .OrderByDescending(t => t.CreatedDate) // Sắp xếp phiếu mới nhất lên đầu
                .ToListAsync();
        }

        // Lấy chi tiết 1 Phiếu kho (Kèm theo danh sách các máy đã quét trong phiếu đó)
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetTransactionDetail(int id)
        {
            var transaction = await _context.InventoryTransactions
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.TransactionId == id);

            if (transaction == null) return NotFound(new { message = "Không tìm thấy phiếu kho!" });

            // Lấy danh sách IMEI thuộc phiếu này (Nếu là phiếu nhập thì tìm Inbound, phiếu xuất thì tìm Outbound)
            var serials = await _context.SerialNumbers
                .Where(s => s.InboundTransactionId == id || s.OutboundTransactionId == id)
                .Select(s => new { s.ImeiCode, s.Product.Name, s.Status, s.ConditionNote })
                .ToListAsync();

            // Trả về một object gộp chung cả thông tin phiếu và danh sách hàng hóa
            return new
            {
                TransactionInfo = transaction,
                Items = serials
            };
        }

        // TODO: CÁC API CÓ NGHIỆP VỤ (THÊM PHIẾU NHẬP/XUẤT, QUÉT IMEI) SẼ CODE Ở ĐÂY SAU
    }
}