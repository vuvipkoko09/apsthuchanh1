using ConnectDB.Models;
using ConnectDB.Modles;
using Microsoft.EntityFrameworkCore;

namespace ConnectDB.Data;
public class AppDbContext : DbContext
{
    // Constructor này bắt buộc phải có để nhận Connection String từ Program.cs
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<Student> Students { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Brand> Brands { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<InventoryTransaction> InventoryTransactions { get; set; }
    public DbSet<SerialNumber> SerialNumbers { get; set; }
    public DbSet<User> Users { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Thiết lập Unique Key cho cột IMEI (Quy tắc BR1)
        modelBuilder.Entity<SerialNumber>()
            .HasIndex(s => s.ImeiCode)
            .IsUnique();
    }
}
