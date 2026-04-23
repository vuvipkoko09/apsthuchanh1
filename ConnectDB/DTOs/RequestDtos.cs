using System.ComponentModel.DataAnnotations;

namespace ConnectDB.DTOs
{
    // DTO cho đăng nhập
    public class LoginRequest
    {
        [Required] public string Username { get; set; }
        [Required] public string Password { get; set; }
    }

    // DTO cho Nhập kho (Inbound)
    public class InboundRequest
    {
        [Required] public int UserId { get; set; } // Nhân viên thao tác
        [Required] public int ProductId { get; set; }
        public string? TransportInfo { get; set; }
        public string? Note { get; set; }
        public string? ReferenceNumber { get; set; }
        public decimal TotalAmount { get; set; }
        [Required]
        [MinLength(1, ErrorMessage = "Phải có ít nhất 1 IMEI")]
        public List<string> Imeis { get; set; }
    }

    // DTO cho Xuất kho (Outbound)
    public class OutboundRequest
    {
        [Required] public int UserId { get; set; } // Nhân viên thao tác
        public string? TransportInfo { get; set; }
        public string? Note { get; set; }
        public string? ReferenceNumber { get; set; }
        public decimal TotalAmount { get; set; }
        [Required]
        [MinLength(1, ErrorMessage = "Phải quét ít nhất 1 IMEI")]
        public List<string> Imeis { get; set; }
    }

}