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
            try 
            {
                if (file == null || file.Length == 0)
                    return BadRequest(new { message = "Không tìm thấy file hợp lệ." });

                using var stream = file.OpenReadStream();
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(file.FileName, stream)
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                if (uploadResult.Error != null)
                {
                    return StatusCode(500, new { message = "Lỗi Cloudinary: " + uploadResult.Error.Message });
                }

                return Ok(new { imageUrl = uploadResult.SecureUrl.ToString() });
            }
            catch (Exception ex) 
            {
                return StatusCode(500, new { message = "Lỗi hệ thống khi upload: " + ex.Message });
            }
        }

        [HttpDelete("image")]
        public async Task<IActionResult> DeleteImage([FromQuery] string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl)) return BadRequest("Url không hợp lệ.");

            try
            {
                var uri = new Uri(imageUrl);
                var segments = uri.Segments;
                var fileName = segments.Last();
                var publicId = "WMS_Products/" + Path.GetFileNameWithoutExtension(fileName);

                var deletionParams = new DeletionParams(publicId);
                var result = await _cloudinary.DestroyAsync(deletionParams);

                return Ok(new { result = result.Result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi xóa ảnh: " + ex.Message });
            }
        }
    }
}