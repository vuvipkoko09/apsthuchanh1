using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ConnectDB.Models;
using ConnectDB.Data;
using Microsoft.AspNetCore.Authorization;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace ConnectDB.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Bắt buộc đăng nhập để truy cập mọi endpoint
    public class ProductsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly Cloudinary _cloudinary;

        public ProductsController(AppDbContext context, IConfiguration config)
        {
            _context = context;

            // Khởi tạo Cloudinary cho việc dọn dẹp ảnh
            var account = new Account(
                config["Cloudinary:CloudName"],
                config["Cloudinary:ApiKey"],
                config["Cloudinary:ApiSecret"]
            );
            _cloudinary = new Cloudinary(account);
        }

        // ==========================================
        // 1. READ (LẤY DỮ LIỆU) - Tối ưu với AsNoTracking()
        // ==========================================

        [HttpGet]
        [AllowAnonymous] // Mở khóa tạm thời để debug
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
        [Authorize(Roles = "Admin,Staff")] // Chỉ Admin và Nhân viên mới được tạo sản phẩm
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

            await LogAction("CREATE", product.ProductId.ToString(), $"Thêm sản phẩm mới: {product.Name} (SKU: {product.SKU})");

            return CreatedAtAction(nameof(GetProductById), new { id = product.ProductId }, product);
        }

        [HttpPost("bulk")]
        [Authorize(Roles = "Admin")]
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
        [Authorize(Roles = "Admin,Staff")] // Chỉ Admin và Nhân viên mới được cập nhật sản phẩm
        public async Task<IActionResult> UpdateProduct(int id, Product product)
        {
            if (id != product.ProductId)
                return BadRequest(new { message = "ID trên URL và ID trong body không khớp!" });

            var existingProduct = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.ProductId == id);
            if (existingProduct == null) return NotFound(new { message = "Sản phẩm không tồn tại!" });

            if (await _context.Products.AnyAsync(p => p.SKU == product.SKU && p.ProductId != id))
            {
                return BadRequest(new { message = "Mã SKU này đã được sử dụng cho sản phẩm khác!" });
            }

            // Dọn dẹp ảnh cũ nếu ảnh mới khác ảnh cũ
            if (!string.IsNullOrEmpty(existingProduct.ImageUrl) && existingProduct.ImageUrl != product.ImageUrl)
            {
                await DeleteImageAsync(existingProduct.ImageUrl);
            }

            // Đảm bảo không update nhầm các bảng liên kết
            product.Category = null;
            product.Brand = null;
            product.SerialNumbers = null;

            product.UpdatedAt = DateTime.Now;
            _context.Entry(product).State = EntityState.Modified;
            
            // Bảo vệ các trường không nên bị ghi đè ngẫu nhiên
            _context.Entry(product).Property(x => x.CreatedAt).IsModified = false;

            try
            {
                await _context.SaveChangesAsync();
                await LogAction("UPDATE", id.ToString(), $"Cập nhật sản phẩm {existingProduct.Name} ({existingProduct.SKU})");
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
        [Authorize(Roles = "Admin,Staff")] // Chỉ Admin và Nhân viên mới được xóa sản phẩm
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound(new { message = "Không tìm thấy sản phẩm!" });

            bool hasInventory = await _context.SerialNumbers.AnyAsync(s => s.ProductId == id);
            if (hasInventory)
            {
                return BadRequest(new { message = "Không thể xóa sản phẩm đã có dữ liệu nhập/xuất trong kho!" });
            }

            // Dọn dẹp ảnh trên Cloudinary
            if (!string.IsNullOrEmpty(product.ImageUrl))
            {
                await DeleteImageAsync(product.ImageUrl);
            }

            var productName = product.Name;
            var productSku = product.SKU;

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            await LogAction("DELETE", id.ToString(), $"Xóa sản phẩm: {productName} (SKU: {productSku})");

            return Ok(new { message = "Đã xóa sản phẩm thành công!" });
        }

        [HttpDelete("bulk")]
        [Authorize(Roles = "Admin")]
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

            // Dọn dẹp ảnh hàng loạt
            foreach (var p in productsToDelete)
            {
                if (!string.IsNullOrEmpty(p.ImageUrl))
                {
                    await DeleteImageAsync(p.ImageUrl);
                }
            }

            _context.Products.RemoveRange(productsToDelete);
            await _context.SaveChangesAsync();

            await LogAction("BULK_DELETE", string.Join(",", ids), $"Đã xóa {productsToDelete.Count} sản phẩm.");

            return Ok(new { message = $"Đã xóa thành công {productsToDelete.Count} sản phẩm!" });
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
                EntityName = "Product",
                EntityId = entityId,
                Changes = changes,
                Timestamp = DateTime.Now
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.ProductId == id);
        }

        private async Task DeleteImageAsync(string imageUrl)
        {
            try
            {
                var uri = new Uri(imageUrl);
                var fileName = uri.Segments.Last();
                var publicId = "WMS_Products/" + Path.GetFileNameWithoutExtension(fileName);
                await _cloudinary.DestroyAsync(new DeletionParams(publicId));
            }
            catch { /* Ignore errors to prevent failing the main transaction */ }
        }
    }
}