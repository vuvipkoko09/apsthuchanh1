using ConnectDB.Data;
using ConnectDB.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace ConnectDB.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AuthController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            // Tìm User trong DB
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == request.Username && u.PasswordHash == request.Password);

            if (user == null)
            {
                return Unauthorized(new { message = "Sai tên đăng nhập hoặc mật khẩu!" });
            }

            // Đăng nhập thành công (Thực tế sẽ trả về Token JWT, ở mức MVP ta trả về thông tin User)
            return Ok(new
            {
                message = "Đăng nhập thành công!",
                userId = user.UserId,
                fullName = user.FullName,
                role = user.Role
            });
        }
    }
}