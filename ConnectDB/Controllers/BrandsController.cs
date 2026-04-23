using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ConnectDB.Models;
using ConnectDB.Data;
using Microsoft.AspNetCore.Authorization;

namespace ConnectDB.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class BrandsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public BrandsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Brands
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<Brand>>> GetBrands()
        {
            return await _context.Brands.ToListAsync();
        }

        // GET: api/Brands/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Brand>> GetBrand(int id)
        {
            var brand = await _context.Brands.FindAsync(id);

            if (brand == null) return NotFound(new { message = "Không tìm thấy thương hiệu!" });

            return brand;
        }

        // POST: api/Brands
        [HttpPost]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<ActionResult<Brand>> CreateBrand(Brand brand)
        {
            _context.Brands.Add(brand);
            await _context.SaveChangesAsync();

            await LogAction("CREATE", brand.BrandId.ToString(), $"Thêm thương hiệu mới: {brand.BrandName}");

            return CreatedAtAction(nameof(GetBrand), new { id = brand.BrandId }, brand);
        }

        // PUT: api/Brands/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> UpdateBrand(int id, Brand brand)
        {
            if (id != brand.BrandId) return BadRequest(new { message = "ID không hợp lệ!" });

            var oldBrand = await _context.Brands.AsNoTracking().FirstOrDefaultAsync(b => b.BrandId == id);
            if (oldBrand == null) return NotFound();

            brand.UpdatedAt = DateTime.Now;
            _context.Entry(brand).State = EntityState.Modified;
            _context.Entry(brand).Property(x => x.CreatedAt).IsModified = false;

            try
            {
                await _context.SaveChangesAsync();
                await LogAction("UPDATE", id.ToString(), $"Cập nhật thương hiệu: {oldBrand.BrandName} -> {brand.BrandName}");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!BrandExists(id)) return NotFound();
                else throw;
            }

            return Ok(new { message = "Cập nhật thương hiệu thành công!" });
        }

        // DELETE: api/Brands/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> DeleteBrand(int id)
        {
            var brand = await _context.Brands.FindAsync(id);
            if (brand == null) return NotFound();

            // LUẬT NGHIỆP VỤ: Kiểm tra xem có Product nào đang dùng Brand này không
            bool hasProducts = await _context.Products.AnyAsync(p => p.BrandId == id);
            if (hasProducts)
            {
                return BadRequest(new { message = "Không thể xóa! Đang có sản phẩm thuộc thương hiệu này." });
            }

            var brandBrandName = brand.BrandName;
            _context.Brands.Remove(brand);
            await _context.SaveChangesAsync();

            await LogAction("DELETE", id.ToString(), $"Xóa thương hiệu: {brandBrandName}");

            return Ok(new { message = "Đã xóa thương hiệu thành công!" });
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
                EntityName = "Brand",
                EntityId = entityId,
                Changes = changes,
                Timestamp = DateTime.Now
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }

        private bool BrandExists(int id)
        {
            return _context.Brands.Any(e => e.BrandId == id);
        }
    }
}