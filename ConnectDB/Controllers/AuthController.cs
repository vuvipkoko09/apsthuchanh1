using ConnectDB.Data;
using ConnectDB.DTOs;
using ConnectDB.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ConnectDB.Services;

namespace ConnectDB.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        private readonly IEmailService _emailService;

        public AuthController(AppDbContext context, IConfiguration config, IEmailService emailService)
        {
            _context = context;
            _config = config;
            _emailService = emailService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            // Tìm User trong DB
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == request.Username);

            // Kiểm tra tồn tại
            if (user == null)
            {
                return NotFound(new { message = "Tài khoản này chưa được đăng ký trên hệ thống!" });
            }

            // Xác minh password với BCrypt
            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return Unauthorized(new { message = "Mật khẩu không chính xác!" });
            }

            // === Tạo JWT Token ===
            var jwtSettings = _config.GetSection("Jwt");
            var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("WMS_SuperSecretKey_32Characters!@#"));
            var credentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim("fullName", user.FullName ?? ""),
                new Claim(ClaimTypes.Role, user.Role ?? "Staff"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: "WMSApp",
                audience: "WMSFrontend",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: credentials
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            return Ok(new
            {
                message = "Đăng nhập thành công!",
                token = tokenString,
                userId = user.UserId,
                username = user.Username,
                fullName = user.FullName,
                role = user.Role
            });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] User user)
        {
            // Kiểm tra trùng username
            if (await _context.Users.AnyAsync(u => u.Username == user.Username))
                return BadRequest(new { message = "Username này đã có người sử dụng!" });

            // Kiểm tra trùng email
            if (await _context.Users.AnyAsync(u => u.Email == user.Email))
                return BadRequest(new { message = "Email này đã được đăng ký!" });

            // Mã hóa mật khẩu
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);

            // Nếu không cung cấp role, mặc định là Admin (để phục vụ quá trình test hệ thống)
            if (string.IsNullOrEmpty(user.Role))
                user.Role = "Admin";

            // Thiết lập các giá trị mặc định cho user mới
            user.CreatedAt = DateTime.Now;
            user.IsDeleted = false;
            user.Status = "Active";

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đăng ký thành công!" });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
            {
                // Vì lý do bảo mật, không báo lỗi nếu email không tồn tại
                return Ok(new { message = "Nếu email tồn tại trong hệ thống, bạn sẽ nhận được liên kết đặt lại mật khẩu." });
            }

            // Tạo Token ngẫu nhiên
            var token = Guid.NewGuid().ToString();
            user.PasswordResetToken = token;
            user.ResetTokenExpiry = DateTime.Now.AddHours(2);

            await _context.SaveChangesAsync();

            try 
            {
                // Gửi Email
                var resetLink = $"{_config["AppUrl"]}/reset-password?token={token}";
                var emailBody = $@"
                    <div style='font-family: Arial, sans-serif; padding: 20px; border: 1px solid #eee; border-radius: 10px;'>
                        <h2 style='color: #1890ff;'>Yêu cầu đặt lại mật khẩu</h2>
                        <p>Chào bạn, chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản <b>{user.Username}</b>.</p>
                        <p>Vui lòng nhấn vào nút bên dưới để thực hiện:</p>
                        <div style='text-align: center; margin: 30px 0;'>
                            <a href='{resetLink}' style='padding: 12px 24px; background-color: #1890ff; color: white; text-decoration: none; border-radius: 5px; font-weight: bold;'>Đặt lại mật khẩu</a>
                        </div>
                        <p style='color: #8c8c8c; font-size: 12px;'>Nếu không phải bạn yêu cầu, hãy bỏ qua email này. Liên kết sẽ hết hạn sau 2 giờ.</p>
                    </div>";

                await _emailService.SendEmailAsync(user.Email, "Đặt lại mật khẩu - WMS System", emailBody);
                return Ok(new { message = "Liên kết đặt lại mật khẩu đã được gửi đến email của bạn." });
            }
            catch (Exception ex)
            {
                // Nếu lỗi do cấu hình email sai
                return StatusCode(500, new { message = "Lỗi khi gửi email: " + ex.Message });
            }
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => 
                u.PasswordResetToken == request.Token && 
                u.ResetTokenExpiry > DateTime.Now);

            if (user == null)
            {
                return BadRequest(new { message = "Liên kết không hợp lệ hoặc đã hết hạn!" });
            }

            // Cập nhật mật khẩu mới
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.PasswordResetToken = null;
            user.ResetTokenExpiry = null;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Mật khẩu đã được đặt lại thành công! Bạn có thể đăng nhập bằng mật khẩu mới." });
        }
    }
}