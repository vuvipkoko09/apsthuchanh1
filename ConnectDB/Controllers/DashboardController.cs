    using ConnectDB.Data;
    using ConnectDB.Models;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;

    using Microsoft.AspNetCore.Authorization;

    namespace ConnectDB.Controllers
    {
        [Route("api/[controller]")]
        [ApiController]
        [Authorize]
        public class DashboardController : ControllerBase
        {
            private readonly AppDbContext _context;

            public DashboardController(AppDbContext context)
            {
                _context = context;
            }

            [HttpGet("stats")]
            public async Task<IActionResult> GetStats()
            {
                try
                {
                    var totalProducts = await _context.Products.CountAsync();
                    var totalCategories = await _context.Categories.CountAsync();
                    var totalBrands = await _context.Brands.CountAsync();
                    var totalUsers = await _context.Users.CountAsync();

                    var stockCount = await _context.SerialNumbers.CountAsync(s => s.Status == "IN_STOCK");
                    var soldCount = await _context.SerialNumbers.CountAsync(s => s.Status == "SOLD");

                    var inboundCount = await _context.InventoryTransactions.CountAsync(t => t.Type == "INBOUND");
                    var outboundCount = await _context.InventoryTransactions.CountAsync(t => t.Type == "OUTBOUND");

                    // Xuất kho HÔM NAY
                    var today = DateTime.Today;
                    var outboundToday = await _context.InventoryTransactions
                        .CountAsync(t => t.Type == "OUTBOUND" && t.CreatedAt >= today);

                    // Dữ liệu biểu đồ 7 ngày gần nhất (dựa trên thực tế)
                    var sevenDaysAgo = today.AddDays(-6);
                    var allTransactions = await _context.InventoryTransactions
                        .Where(t => t.CreatedAt >= sevenDaysAgo)
                        .Select(t => new { t.Type, t.CreatedAt })
                        .ToListAsync();

                    var chartData = Enumerable.Range(0, 7).Select(i =>
                    {
                        var date = sevenDaysAgo.AddDays(i);
                        var label = date.ToString("ddd"); // Mon, Tue, ...
                        var inbound = allTransactions.Count(t => t.Type == "INBOUND" && t.CreatedAt.Date == date.Date);
                        var outbound = allTransactions.Count(t => t.Type == "OUTBOUND" && t.CreatedAt.Date == date.Date);
                        return new { label, inbound, outbound };
                    }).ToList();

                    var recentTransactions = await _context.InventoryTransactions
                        .Include(t => t.User)
                        .OrderByDescending(t => t.CreatedAt)
                        .Take(10)
                        .Select(t => new
                        {
                            t.TransactionId,
                            t.Type,
                            t.CreatedAt,
                            UserName = t.User != null ? t.User.FullName : "System",
                            t.Note
                        })
                        .ToListAsync();

                    // Dữ liệu cho biểu đồ phân bổ sản phẩm theo danh mục
                    var categoryDistribution = await _context.Categories
                        .Select(c => new
                        {
                            Name = c.CategoryName,
                            Count = _context.Products.Count(p => p.CategoryId == c.CategoryId)
                        })
                        .ToListAsync();

                    // Lấy danh sách hàng sắp hết (Sản phẩm có tồn kho < 5)
                    var lowStockItems = await _context.Products
                        .Select(p => new
                        {
                            Id = p.ProductId,
                            Name = p.Name,
                            Sku = p.SKU,
                            Image = p.ImageUrl,
                            Stock = _context.SerialNumbers.Count(s => s.ProductId == p.ProductId && s.Status == "IN_STOCK")
                        })
                        .Where(p => p.Stock < 5)
                        .OrderBy(p => p.Stock)
                        .Take(10)
                        .ToListAsync();

                    return Ok(new
                    {
                        TotalProducts = totalProducts,
                        TotalCategories = totalCategories,
                        TotalBrands = totalBrands,
                        TotalUsers = totalUsers,
                        StockCount = stockCount,
                        SoldCount = soldCount,
                        InboundCount = inboundCount,
                        OutboundCount = outboundCount,
                        OutboundToday = outboundToday,
                        ChartData = chartData,
                        RecentTransactions = recentTransactions,
                        CategoryDistribution = categoryDistribution,
                        LowStockItems = lowStockItems
                    });
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { message = "Lỗi hệ thống: " + ex.Message });
                }
            }
        }
    }
