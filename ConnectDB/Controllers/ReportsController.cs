using ConnectDB.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ConnectDB.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ReportsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ReportsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("inventory-summary")]
        public async Task<ActionResult<object>> GetInventorySummary()
        {
            var products = await _context.Products.Include(p => p.Category).ToListAsync();
            // Dem so Serial Number co trang thai "IN_STOCK" (Fixing casing bug)
            var serials = await _context.SerialNumbers.Where(s => s.Status == "IN_STOCK").ToListAsync();

            var totalItemsInStock = serials.Count;
            
            // Tính tổng giá trị tồn kho (Dựa trên giá vốn CostPrice)
            decimal totalValue = 0;
            foreach(var s in serials)
            {
                var prod = products.FirstOrDefault(p => p.ProductId == s.ProductId);
                if (prod != null) totalValue += prod.CostPrice;
            }

            var categoryDistribution = products.GroupBy(p => p.Category?.CategoryName ?? "Others")
                .Select(g => new { 
                    Category = g.Key, 
                    ProductCount = g.Count(),
                    ItemCount = serials.Count(s => g.Select(p => p.ProductId).Contains(s.ProductId)),
                    Value = serials
                        .Where(s => g.Select(p => p.ProductId).Contains(s.ProductId))
                        .Sum(s => products.FirstOrDefault(p => p.ProductId == s.ProductId)?.CostPrice ?? 0)
                })
                .Where(x => x.ItemCount > 0)
                .OrderByDescending(x => x.Value);

            return new {
                TotalItemsInStock = totalItemsInStock,
                TotalInventoryValue = totalValue,
                TotalCategories = categoryDistribution.Count(),
                CategoryDistribution = categoryDistribution
            };
        }

        [HttpGet("low-stock")]
        public async Task<ActionResult<object>> GetLowStockItems([FromQuery] int threshold = 5)
        {
            // Trong ung dung thuc te, Product.Stock se the hien so ton kho,
            // nhung hien tai ta phu thuoc vao SerialNumber "IN_STOCK"
            var inStockSerials = await _context.SerialNumbers
                .Where(s => s.Status == "IN_STOCK")
                .GroupBy(s => s.ProductId)
                .Select(g => new { ProductId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.ProductId, g => g.Count);

            var products = await _context.Products.ToListAsync();
            var lowStockList = new List<object>();

            foreach(var p in products)
            {
                var stock = inStockSerials.ContainsKey(p.ProductId) ? inStockSerials[p.ProductId] : 0;
                if (stock <= threshold)
                {
                    lowStockList.Add(new {
                        p.ProductId,
                        p.Name,
                        p.SKU,
                        Stock = stock
                    });
                }
            }

            return Ok(lowStockList.OrderBy(p => (int)((dynamic)p).Stock));
        }
    }
}
