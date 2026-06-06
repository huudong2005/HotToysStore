using System;
using Microsoft.EntityFrameworkCore;
using ToyStore.Domain.Entities;

namespace ToyStore.Infrastructure.Data;

public partial class ToyStoreContext : DbContext
{
    public ToyStoreContext()
    {
    }

    public ToyStoreContext(DbContextOptions<ToyStoreContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Admin> Admins { get; set; }
    public virtual DbSet<Cart> Carts { get; set; }
    public virtual DbSet<CartItem> CartItems { get; set; }
    public virtual DbSet<Category> Categories { get; set; }
    public virtual DbSet<Customer> Customers { get; set; }
    public virtual DbSet<Order> Orders { get; set; }
    public virtual DbSet<OrderDetail> OrderDetails { get; set; }
    public virtual DbSet<Product> Products { get; set; }
    public virtual DbSet<Promotion> Promotions { get; set; }
    public virtual DbSet<Banner> Banners { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Connection string will be configured in Program.cs
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Admin>(entity =>
        {
            entity.HasKey(e => e.AdminId).HasName("PK_Admin");
            entity.ToTable("Admin");
            entity.HasIndex(e => e.Username, "UQ_Admin_Username").IsUnique();

            entity.Property(e => e.AdminId).HasColumnName("AdminID");
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.Role)
                .HasMaxLength(50)
                .HasDefaultValue("Staff");
            entity.Property(e => e.Username).HasMaxLength(50);
        });

        modelBuilder.Entity<Cart>(entity =>
        {
            entity.HasKey(e => e.CartId).HasName("PK_Cart");
            entity.ToTable("Cart");

            entity.Property(e => e.CartId).HasColumnName("CartID");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP") // Đã sửa cho Oracle
                .HasColumnType("TIMESTAMP");             // Đã sửa cho Oracle
            entity.Property(e => e.CustomerId).HasColumnName("CustomerID");

            entity.HasOne(d => d.Customer).WithMany(p => p.Carts)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.HasKey(e => e.CartItemId).HasName("PK_CartItem");
            entity.ToTable("CartItem");

            entity.Property(e => e.CartItemId).HasColumnName("CartItemID");
            entity.Property(e => e.CartId).HasColumnName("CartID");
            entity.Property(e => e.ProductId).HasColumnName("ProductID");

            entity.HasOne(d => d.Cart).WithMany(p => p.CartItems)
                .HasForeignKey(d => d.CartId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Product).WithMany(p => p.CartItems)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK_Category");
            entity.ToTable("Category");

            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.CategoryName).HasMaxLength(100);
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.CustomerId).HasName("PK_Customer");
            entity.ToTable("Customer");
            entity.HasIndex(e => e.Email, "UQ_Customer_Email").IsUnique();

            entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP") // Đã sửa cho Oracle
                .HasColumnType("TIMESTAMP");             // Đã sửa cho Oracle
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.Phone).HasMaxLength(20);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("PK_Orders");
            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
            entity.Property(e => e.DeliveryMethod).HasMaxLength(50);
            entity.Property(e => e.OrderDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP") // Đã sửa cho Oracle
                .HasColumnType("TIMESTAMP");             // Đã sửa cho Oracle
            entity.Property(e => e.PaymentMethod).HasMaxLength(50);
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("Pending");
            entity.Property(e => e.TotalAmount).HasColumnType("NUMBER(12, 2)"); // Kiểu decimal của Oracle
            entity.Property(e => e.Subtotal).HasColumnType("NUMBER(12, 2)").HasDefaultValue(0);
            entity.Property(e => e.DiscountValue).HasColumnType("NUMBER(12, 2)").HasDefaultValue(0);
            entity.Property(e => e.DiscountStrategyName).HasMaxLength(50);

            entity.HasOne(d => d.Customer).WithMany(p => p.Orders)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<OrderDetail>(entity =>
        {
            entity.HasKey(e => e.OrderDetailId).HasName("PK_OrderDetail");
            entity.ToTable("OrderDetail");

            entity.Property(e => e.OrderDetailId).HasColumnName("OrderDetailID");
            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.UnitPrice).HasColumnType("NUMBER(12, 2)");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderDetails)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Product).WithMany(p => p.OrderDetails)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.ProductId).HasName("PK_Product");
            entity.ToTable("Product");

            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(255)
                .HasColumnName("ImageURL");
            entity.Property(e => e.Price).HasColumnType("NUMBER(12, 2)");
            entity.Property(e => e.ProductName).HasMaxLength(200);
            entity.Property(e => e.Status)
            
          .HasColumnType("NUMBER(1)")       // Chỉ định rõ kiểu số trong Oracle
          .HasDefaultValueSql("1");         // Sử dụng lệnh SQL gán giá trị 1 (tương đương true)

            entity.HasOne(d => d.Category).WithMany(p => p.Products)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Promotion>(entity =>
        {
            // Map khớp 100% DDL Oracle (tên bảng/cột đặt trong dấu ngoặc kép -> phân biệt hoa/thường)
            entity.HasKey(e => e.PromotionId).HasName("PK_Promotion");
            entity.ToTable("Promotion");
            entity.HasIndex(e => e.PromotionCode, "UQ_Promotion_Code").IsUnique();

            entity.Property(e => e.PromotionId).HasColumnName("PromotionID");
            entity.Property(e => e.PromotionCode).HasColumnName("PromotionCode").HasMaxLength(50);
            entity.Property(e => e.PromotionName).HasColumnName("PromotionName").HasMaxLength(255);
            entity.Property(e => e.DiscountType).HasColumnName("DiscountType").HasMaxLength(50);
            entity.Property(e => e.DiscountValue).HasColumnName("DiscountValue").HasColumnType("NUMBER(18, 2)");
            entity.Property(e => e.MinOrderValue).HasColumnName("MinOrderValue").HasColumnType("NUMBER(18, 2)").HasDefaultValue(0);
            entity.Property(e => e.StartDate).HasColumnName("StartDate").HasColumnType("TIMESTAMP");
            entity.Property(e => e.EndDate).HasColumnName("EndDate").HasColumnType("TIMESTAMP");
            entity.Property(e => e.UsageLimit).HasColumnName("UsageLimit").HasDefaultValue(0);
            entity.Property(e => e.UsedCount).HasColumnName("UsedCount").HasDefaultValue(0);
            // Converter tường minh bool <-> NUMBER(1): luôn gửi 0/1 thay vì literal TRUE/FALSE.
            // Không đặt HasDefaultValueSql ở tầng model để EF không bỏ qua cột khi IsActive = false
            // (DB vẫn giữ DEFAULT 1 theo DDL khi cột không được gửi lên).
            entity.Property(e => e.IsActive)
                .HasColumnName("IsActive")
                .HasColumnType("NUMBER(1)");
                
        });

        modelBuilder.Entity<Banner>(entity =>
        {
            entity.HasKey(e => e.BannerId).HasName("PK_Banner");
            entity.ToTable("Banner");

            entity.Property(e => e.BannerId).HasColumnName("BannerID");
            entity.Property(e => e.Title).HasColumnName("Title").HasMaxLength(255);
            entity.Property(e => e.ImagePath).HasColumnName("ImagePath").HasMaxLength(500);

            // Oracle: CHỈ dùng NUMBER(1) cho cờ boolean, TUYỆT ĐỐI KHÔNG dùng .HasConversion()
            // để tránh InvalidCastException / ORA-00904.
            entity.Property(e => e.IsActive).HasColumnName("IsActive").HasColumnType("NUMBER(1)");

            entity.Property(e => e.CreatedAt)
                .HasColumnName("CreatedAt")
                .HasColumnType("TIMESTAMP")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}