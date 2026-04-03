using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ConnectDB.Models;
using ConnectDB.Data;

namespace ConnectDB.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        // Lấy danh sách nhân viên
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            return await _context.Users.ToListAsync();
        }

        // Thêm nhân viên mới
        [HttpPost]
        public async Task<ActionResult<User>> CreateUser(User user)
        {
            // Kiểm tra trùng username
            if (await _context.Users.AnyAsync(u => u.Username == user.Username))
                return BadRequest(new { message = "Username này đã có người sử dụng!" });

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUsers), new { id = user.UserId }, user);
        }

        // Sửa thông tin nhân viên
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, User user)
        {
            if (id != user.UserId) return BadRequest("ID không hợp lệ!");

            _context.Entry(user).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Cập nhật thành công!" });
        }

        // Xóa nhân viên
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            // LUẬT: Không được xóa user đã từng làm phiếu kho (chỉ nên khóa tài khoản)
            bool hasTransactions = await _context.InventoryTransactions.AnyAsync(t => t.UserId == id);
            if (hasTransactions)
            {
                return BadRequest(new { message = "Không thể xóa! Nhân viên này đã từng tham gia xuất/nhập kho." });
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã xóa nhân viên thành công!" });
        }
    }
}