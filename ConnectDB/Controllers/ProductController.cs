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
        // 1. READ (LẤY DỮ LIỆU)
        // ==========================================

        // Lấy tất cả sản phẩm
        // GET: api/Products
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetAllProducts()
        {
            // Dùng Include để lấy luôn thông tin Tên danh mục và Tên hãng
            return await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .ToListAsync();
        }

        // Lấy 1 sản phẩm theo ID
        // GET: api/Products/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProductById(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null) return NotFound(new { message = "Không tìm thấy sản phẩm!" });

            return product;
        }

        // Tìm kiếm sản phẩm theo Tên
        // GET: api/Products/search?name=iphone
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<Product>>> SearchProductsByName([FromQuery] string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest(new { message = "Vui lòng nhập tên cần tìm." });

            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Where(p => p.Name.Contains(name) || p.SKU.Contains(name)) // Tìm cả theo Tên và SKU
                .ToListAsync();

            return products;
        }

        // Lấy sản phẩm theo Danh mục (Category)
        // GET: api/Products/category/1
        [HttpGet("category/{categoryId}")]
        public async Task<ActionResult<IEnumerable<Product>>> GetProductsByCategory(int categoryId)
        {
            return await _context.Products
                .Include(p => p.Brand)
                .Where(p => p.CategoryId == categoryId)
                .ToListAsync();
        }

        // Lấy sản phẩm theo Thương hiệu (Brand)
        // GET: api/Products/brand/2
        [HttpGet("brand/{brandId}")]
        public async Task<ActionResult<IEnumerable<Product>>> GetProductsByBrand(int brandId)
        {
            return await _context.Products
                .Include(p => p.Category)
                .Where(p => p.BrandId == brandId)
                .ToListAsync();
        }

        // ==========================================
        // 2. CREATE (THÊM MỚI)
        // ==========================================

        // Thêm 1 sản phẩm
        // POST: api/Products
        [HttpPost]
        public async Task<ActionResult<Product>> CreateProduct(Product product)
        {
            // Kiểm tra trùng SKU
            if (await _context.Products.AnyAsync(p => p.SKU == product.SKU))
            {
                return BadRequest(new { message = "Mã SKU này đã tồn tại!" });
            }

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProductById), new { id = product.ProductId }, product);
        }

        // Thêm NHIỀU sản phẩm cùng lúc (Bulk Insert)
        // POST: api/Products/bulk
        [HttpPost("bulk")]
        public async Task<ActionResult> CreateMultipleProducts(List<Product> products)
        {
            if (products == null || !products.Any())
                return BadRequest(new { message = "Danh sách sản phẩm trống!" });

            // Lấy danh sách SKU để check trùng
            var skus = products.Select(p => p.SKU).ToList();
            var existingSkus = await _context.Products
                .Where(p => skus.Contains(p.SKU))
                .Select(p => p.SKU)
                .ToListAsync();

            if (existingSkus.Any())
            {
                return BadRequest(new { message = "Các mã SKU sau đã tồn tại: " + string.Join(", ", existingSkus) });
            }

            await _context.Products.AddRangeAsync(products);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Đã thêm thành công {products.Count} sản phẩm!" });
        }

        // ==========================================
        // 3. UPDATE (CẬP NHẬT)
        // ==========================================

        // Cập nhật 1 sản phẩm
        // PUT: api/Products/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(int id, Product product)
        {
            if (id != product.ProductId)
                return BadRequest(new { message = "ID trên URL và ID trong body không khớp!" });

            // Check trùng SKU với máy khác
            if (await _context.Products.AnyAsync(p => p.SKU == product.SKU && p.ProductId != id))
            {
                return BadRequest(new { message = "Mã SKU này đã được sử dụng cho sản phẩm khác!" });
            }

            _context.Entry(product).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(id)) return NotFound();
                else throw;
            }

            return Ok(new { message = "Cập nhật thành công!" });
        }

        // ==========================================
        // 4. DELETE (XÓA)
        // ==========================================

        // Xóa 1 sản phẩm
        // DELETE: api/Products/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            // Check xem sản phẩm này đã có trong kho (có SerialNumber) chưa
            bool hasInventory = await _context.SerialNumbers.AnyAsync(s => s.ProductId == id);
            if (hasInventory)
            {
                return BadRequest(new { message = "Không thể xóa sản phẩm đã có dữ liệu nhập/xuất trong kho!" });
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã xóa sản phẩm thành công!" });
        }

        // Xóa NHIỀU sản phẩm cùng lúc
        // DELETE: api/Products/bulk
        [HttpDelete("bulk")]
        public async Task<IActionResult> DeleteMultipleProducts([FromBody] List<int> ids)
        {
            if (ids == null || !ids.Any())
                return BadRequest(new { message = "Danh sách ID trống!" });

            var productsToDelete = await _context.Products.Where(p => ids.Contains(p.ProductId)).ToListAsync();

            if (!productsToDelete.Any())
                return NotFound(new { message = "Không tìm thấy sản phẩm nào để xóa." });

            // Kiểm tra xem có sản phẩm nào đang dính dữ liệu kho không
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

        // Hàm hỗ trợ kiểm tra tồn tại
        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.ProductId == id);
        }
    }
}