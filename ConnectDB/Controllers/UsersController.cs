using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ConnectDB.Models;
using ConnectDB.Data;
using ConnectDB.DTOs;

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

            // Mã hóa mật khẩu
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUsers), new { id = user.UserId }, user);
        }

        // Sửa thông tin nhân viên
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UserUpdateDto updatedUser)
        {
            var existingUser = await _context.Users.FindAsync(id);
            if (existingUser == null) return NotFound(new { message = "Không tìm thấy người dùng!" });

            // Chỉ cập nhật các trường được phép sửa từ trang Profile
            if (updatedUser.FullName != null) existingUser.FullName = updatedUser.FullName;
            if (updatedUser.FirstName != null) existingUser.FirstName = updatedUser.FirstName;
            if (updatedUser.LastName != null) existingUser.LastName = updatedUser.LastName;
            if (updatedUser.Email != null) existingUser.Email = updatedUser.Email;
            if (updatedUser.PhoneNumber != null) existingUser.PhoneNumber = updatedUser.PhoneNumber;
            if (updatedUser.Address != null) existingUser.Address = updatedUser.Address;
            if (updatedUser.Gender != null) existingUser.Gender = updatedUser.Gender;
            
            existingUser.Birthday = updatedUser.Birthday;
            existingUser.UpdatedAt = DateTime.Now;

            // Nếu có gửi Avatar mới thì cập nhật
            if (!string.IsNullOrEmpty(updatedUser.Avatar))
                existingUser.Avatar = updatedUser.Avatar;

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { message = "Cập nhật thành công!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lỗi khi lưu dữ liệu: " + ex.Message });
            }
        }

        // Xóa nhân viên
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            // LUẬT: Nếu nhân viên đã từng tham gia giao dịch, sử dụng Xóa mềm (Soft Delete)
            bool hasTransactions = await _context.InventoryTransactions.AnyAsync(t => t.UserId == id);
            if (hasTransactions)
            {
                user.IsDeleted = true;
                user.DeletedAt = DateTime.Now;
                user.Status = "Deleted";
                await _context.SaveChangesAsync();
                return Ok(new { message = "Nhân viên đã có giao dịch nên đã được chuyển sang trạng thái Đã xóa (Soft Delete)." });
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã xóa nhân viên thành công!" });
        }
    }
}