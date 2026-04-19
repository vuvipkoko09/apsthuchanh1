using Microsoft.AspNetCore.Mvc;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace ConnectDB.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UploadController : ControllerBase
    {
        private readonly Cloudinary _cloudinary;

        public UploadController(IConfiguration config)
        {
            // Lấy thông tin từ appsettings.json để mở khóa Cloudinary
            var account = new Account(
                config["Cloudinary:CloudName"],
                config["Cloudinary:ApiKey"],
                config["Cloudinary:ApiSecret"]
            );
            _cloudinary = new Cloudinary(account);
        }

        [HttpPost("image")]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Không tìm thấy file hợp lệ.");

            // Đọc file gửi lên thành luồng (Stream)
            using var stream = file.OpenReadStream();

            // Đóng gói gửi lên mây
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = "WMS_Products", // Nó sẽ tự tạo thư mục này trên mây cho gọn gàng
                Transformation = new Transformation().Width(800).Height(800).Crop("limit") // Tự động bóp size ảnh cho nhẹ DB
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.Error != null)
                return StatusCode(500, uploadResult.Error.Message);

            // Trả về cái Link ảnh trực tiếp (SecureUrl)
            return Ok(new { imageUrl = uploadResult.SecureUrl.ToString() });
        }
    }
}