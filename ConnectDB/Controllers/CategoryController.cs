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
    public class CategoriesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CategoriesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Categories
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<Category>>> GetCategories()
        {
            return await _context.Categories.ToListAsync();
        }

        // GET: api/Categories/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Category>> GetCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);

            if (category == null) return NotFound(new { message = "Không tìm thấy danh mục!" });

            return category;
        }

        // POST: api/Categories
        [HttpPost]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<ActionResult<Category>> CreateCategory(Category category)
        {
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            await LogAction("CREATE", category.CategoryId.ToString(), $"Thêm danh mục mới: {category.CategoryName}");

            return CreatedAtAction(nameof(GetCategory), new { id = category.CategoryId }, category);
        }

        // PUT: api/Categories/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> UpdateCategory(int id, Category category)
        {
            if (id != category.CategoryId) return BadRequest(new { message = "ID không hợp lệ!" });

            var oldCategory = await _context.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.CategoryId == id);
            if (oldCategory == null) return NotFound();

            category.UpdatedAt = DateTime.Now;
            _context.Entry(category).State = EntityState.Modified;
            _context.Entry(category).Property(x => x.CreatedAt).IsModified = false;

            try
            {
                await _context.SaveChangesAsync();
                await LogAction("UPDATE", id.ToString(), $"Cập nhật danh mục: {oldCategory.CategoryName} -> {category.CategoryName}");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CategoryExists(id)) return NotFound();
                else throw;
            }

            return Ok(new { message = "Cập nhật danh mục thành công!" });
        }

        // DELETE: api/Categories/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();

            // LUẬT NGHIỆP VỤ: Kiểm tra xem có Product nào đang dùng Category này không
            bool hasProducts = await _context.Products.AnyAsync(p => p.CategoryId == id);
            if (hasProducts)
            {
                return BadRequest(new { message = "Không thể xóa! Đang có sản phẩm thuộc danh mục này." });
            }

            var categoryCategoryName = category.CategoryName;
            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            await LogAction("DELETE", id.ToString(), $"Xóa danh mục: {categoryCategoryName}");

            return Ok(new { message = "Đã xóa danh mục thành công!" });
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
                EntityName = "Category",
                EntityId = entityId,
                Changes = changes,
                Timestamp = DateTime.Now
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }

        private bool CategoryExists(int id)
        {
            return _context.Categories.Any(e => e.CategoryId == id);
        }
    }
}