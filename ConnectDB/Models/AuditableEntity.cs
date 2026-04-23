using System;

namespace ConnectDB.Models
{
    // Lớp cha (Abstract) chứa các trường dùng chung cho mọi bảng
    public abstract class AuditableEntity
    {
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        // Phục vụ Xóa mềm (Soft Delete) - Mặc định ban đầu là false (chưa xóa)
        public bool IsDeleted { get; set; } = false;

        public DateTime? DeletedAt { get; set; }
    }
}