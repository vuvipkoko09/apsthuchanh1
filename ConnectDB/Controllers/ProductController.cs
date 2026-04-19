using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ConnectDB.Models;
using ConnectDB.Data;

namespace ConnectDB.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProductsController(AppDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1. READ (LẤY DỮ LIỆU) - Tối ưu với AsNoTracking()
        // ==========================================

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetAllProducts()
        {
            return await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .AsNoTracking() // Tăng tốc độ đọc dữ liệu
                .ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProductById(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null) return NotFound(new { message = "Không tìm thấy sản phẩm!" });

            return product;
        }

        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<Product>>> SearchProductsByName([FromQuery] string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest(new { message = "Vui lòng nhập tên hoặc SKU cần tìm." });

            return await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Where(p => p.Name.Contains(name) || p.SKU.Contains(name))
                .AsNoTracking()
                .ToListAsync();
        }

        [HttpGet("category/{categoryId}")]
        public async Task<ActionResult<IEnumerable<Product>>> GetProductsByCategory(int categoryId)
        {
            return await _context.Products
                .Include(p => p.Brand)
                .Where(p => p.CategoryId == categoryId)
                .AsNoTracking()
                .ToListAsync();
        }

        [HttpGet("brand/{brandId}")]
        public async Task<ActionResult<IEnumerable<Product>>> GetProductsByBrand(int brandId)
        {
            return await _context.Products
                .Include(p => p.Category)
                .Where(p => p.BrandId == brandId)
                .AsNoTracking()
                .ToListAsync();
        }

        // ==========================================
        // 2. CREATE (THÊM MỚI)
        // ==========================================

        [HttpPost]
        public async Task<ActionResult<Product>> CreateProduct(Product product)
        {
            if (await _context.Products.AnyAsync(p => p.SKU == product.SKU))
            {
                return BadRequest(new { message = "Mã SKU này đã tồn tại!" });
            }

            // Ngăn EF Core cố gắng tạo mới Category/Brand nếu frontend lỡ truyền kèm Object vào
            product.Category = null;
            product.Brand = null;
            product.SerialNumbers = null;

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProductById), new { id = product.ProductId }, product);
        }

        [HttpPost("bulk")]
        public async Task<ActionResult> CreateMultipleProducts(List<Product> products)
        {
            if (products == null || !products.Any())
                return BadRequest(new { message = "Danh sách sản phẩm trống!" });

            var skus = products.Select(p => p.SKU).ToList();
            var existingSkus = await _context.Products
                .Where(p => skus.Contains(p.SKU))
                .Select(p => p.SKU)
                .ToListAsync();

            if (existingSkus.Any())
            {
                return BadRequest(new { message = "Các mã SKU sau đã tồn tại: " + string.Join(", ", existingSkus) });
            }

            // Xóa rỗng các object tham chiếu để an toàn
            foreach (var p in products)
            {
                p.Category = null;
                p.Brand = null;
                p.SerialNumbers = null;
            }

            await _context.Products.AddRangeAsync(products);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Đã thêm thành công {products.Count} sản phẩm!" });
        }

        // ==========================================
        // 3. UPDATE (CẬP NHẬT)
        // ==========================================

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(int id, Product product)
        {
            if (id != product.ProductId)
                return BadRequest(new { message = "ID trên URL và ID trong body không khớp!" });

            if (await _context.Products.AnyAsync(p => p.SKU == product.SKU && p.ProductId != id))
            {
                return BadRequest(new { message = "Mã SKU này đã được sử dụng cho sản phẩm khác!" });
            }

            // Đảm bảo không update nhầm các bảng liên kết
            product.Category = null;
            product.Brand = null;
            product.SerialNumbers = null;

            _context.Entry(product).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(id)) return NotFound(new { message = "Sản phẩm không tồn tại!" });
                else throw;
            }

            return Ok(new { message = "Cập nhật thành công!" });
        }

        // ==========================================
        // 4. DELETE (XÓA)
        // ==========================================

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound(new { message = "Không tìm thấy sản phẩm!" });

            bool hasInventory = await _context.SerialNumbers.AnyAsync(s => s.ProductId == id);
            if (hasInventory)
            {
                return BadRequest(new { message = "Không thể xóa sản phẩm đã có dữ liệu nhập/xuất trong kho!" });
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã xóa sản phẩm thành công!" });
        }

        [HttpDelete("bulk")]
        public async Task<IActionResult> DeleteMultipleProducts([FromBody] List<int> ids)
        {
            if (ids == null || !ids.Any())
                return BadRequest(new { message = "Danh sách ID trống!" });

            var productsToDelete = await _context.Products.Where(p => ids.Contains(p.ProductId)).ToListAsync();

            if (!productsToDelete.Any())
                return NotFound(new { message = "Không tìm thấy sản phẩm nào để xóa." });

            var existingProductIds = productsToDelete.Select(p => p.ProductId).ToList();
            bool hasInventory = await _context.SerialNumbers.AnyAsync(s => existingProductIds.Contains(s.ProductId));

            if (hasInventory)
            {
                return BadRequest(new { message = "Có sản phẩm trong danh sách đang chứa dữ liệu kho, không thể xóa hàng loạt!" });
            }

            _context.Products.RemoveRange(productsToDelete);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Đã xóa thành công {productsToDelete.Count} sản phẩm!" });
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.ProductId == id);
        }
    }
}