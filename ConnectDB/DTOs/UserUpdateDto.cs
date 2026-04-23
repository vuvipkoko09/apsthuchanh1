using System.ComponentModel.DataAnnotations;

namespace ConnectDB.DTOs
{
    public class UserUpdateDto
    {
        public string? FullName { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string? Avatar { get; set; }
        public string? Gender { get; set; }
        public DateTime? Birthday { get; set; }
    }
}
