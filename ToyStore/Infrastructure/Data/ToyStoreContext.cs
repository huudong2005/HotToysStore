using System;
using Microsoft.EntityFrameworkCore;
using Oracle.EntityFrameworkCore.Metadata;
using ToyStore.Domain.Entities;
using ToyStore.Helpers;

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
    public virtual DbSet<ProductImage> ProductImages { get; set; }
    public virtual DbSet<Promotion> Promotions { get; set; }
    public virtual DbSet<Banner> Banners { get; set; }
    public virtual DbSet<MembershipTier> MembershipTiers { get; set; }
    public virtual DbSet<ChatSession> ChatSessions { get; set; }
    public virtual DbSet<ChatMessage> ChatMessages { get; set; }
    public virtual DbSet<UserActivityLog> UserActivityLogs { get; set; }
    public virtual DbSet<WorkShift> WorkShifts { get; set; }
    public virtual DbSet<Payroll> Payrolls { get; set; }
    public virtual DbSet<GoodsReceipt> GoodsReceipts { get; set; }
    public virtual DbSet<GoodsReceiptDetail> GoodsReceiptDetails { get; set; }
    public virtual DbSet<ProductPriceHistory> ProductPriceHistories { get; set; }

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

            // Hạng thành viên: tên cột map đúng theo DDL Oracle (phân biệt hoa/thường).
            entity.Property(e => e.TotalCompletedOrders)
                .HasColumnName("TotalCompletedOrders")
                .HasColumnType("NUMBER")
                .HasDefaultValue(0);
            entity.Property(e => e.TierId)
                .HasColumnName("TierID")
                .HasColumnType("NUMBER");

            // Oracle: NUMBER(1) cho cờ boolean — KHÔNG dùng .HasConversion().
            entity.Property(e => e.IsLocked)
                .HasColumnName("IsLocked")
                .HasColumnType("NUMBER(1)")
                .HasDefaultValueSql("0");

            entity.HasOne(d => d.Tier)
                .WithMany(t => t.Customers)
                .HasForeignKey(d => d.TierId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Customer_MembershipTier");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("PK_Orders");
            entity.Property(e => e.OrderId).HasColumnName("OrderID");
            entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
            entity.Property(e => e.DeliveryMethod).HasMaxLength(50);
            entity.Property(e => e.ShippingCode)
                .HasColumnName("ShippingCode")
                .HasMaxLength(50);
            entity.Property(e => e.ShippingFee)
                .HasColumnName("ShippingFee")
                .HasColumnType("NUMBER(12, 2)")
                .HasDefaultValue(0);
            entity.Property(e => e.ShippingAddress)
                .HasColumnName("ShippingAddress")
                .HasMaxLength(500);
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
            entity.Property(e => e.MembershipDiscountValue)
                .HasColumnName("MembershipDiscountValue")
                .HasColumnType("NUMBER(12, 2)")
                .HasDefaultValue(0);
            entity.Property(e => e.OrderType)
                .HasColumnName("OrderType")
                .HasMaxLength(20)
                .HasDefaultValue("Online");

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
            entity.Property(e => e.ProductName).HasColumnName("ProductName").HasMaxLength(200);
            entity.Property(e => e.Description).HasColumnName("Description").HasMaxLength(500);
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(255)
                .HasColumnName("ImageURL");
            entity.Property(e => e.Price).HasColumnName("Price").HasColumnType("NUMBER(12, 2)");
            entity.Property(e => e.Stock).HasColumnName("Stock");
            entity.Property(e => e.Status)
                .HasColumnName("Status")
                .HasColumnType("NUMBER(1)")
                .HasDefaultValueSql("1");

            entity.HasOne(d => d.Category).WithMany(p => p.Products)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<ProductImage>(entity =>
        {
            entity.HasKey(e => e.ImageId).HasName("PK_ProductImage");
            entity.ToTable("ProductImage");

            entity.Property(e => e.ImageId).HasColumnName("ImageId");
            entity.Property(e => e.ProductId).HasColumnName("ProductId");
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(255)
                .HasColumnName("ImageUrl");
            entity.Property(e => e.DisplayOrder)
                .HasColumnName("DisplayOrder")
                .HasDefaultValue(0);
        });

        modelBuilder.Entity<ProductImage>()
            .HasOne(p => p.Product)
            .WithMany(p => p.ProductImages)
            .HasForeignKey(p => p.ProductId)
            .HasConstraintName("FK_ProductImage_Product")
            .OnDelete(DeleteBehavior.Cascade);

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

        modelBuilder.Entity<MembershipTier>(entity =>
        {
            // Map khớp DDL Oracle: tên bảng/cột phân biệt hoa/thường.
            entity.HasKey(e => e.TierId).HasName("PK_MembershipTier");
            entity.ToTable("MembershipTier");

            entity.Property(e => e.TierId).HasColumnName("TierID");
            entity.Property(e => e.TierName).HasColumnName("TierName").HasMaxLength(100);
            entity.Property(e => e.RequiredOrders)
                .HasColumnName("RequiredOrders")
                .HasColumnType("NUMBER")
                .HasDefaultValue(0);
            entity.Property(e => e.DiscountPercent)
                .HasColumnName("DiscountPercent")
                .HasColumnType("NUMBER(5, 2)")
                .HasDefaultValue(0);
        });

        modelBuilder.Entity<ChatSession>(entity =>
        {
            entity.HasKey(e => e.SessionId).HasName("PK_ChatSession");
            entity.ToTable("ChatSession");

            entity.Property(e => e.SessionId)
                .HasColumnName("SessionId")
                .UseIdentityColumn();
            entity.Property(e => e.CustomerName).HasColumnName("CustomerName").HasMaxLength(100);
            entity.Property(e => e.Status)
                .HasColumnName("Status")
                .HasMaxLength(50)
                .HasDefaultValue("Pending");
            entity.Property(e => e.CreatedAt)
                .HasColumnName("CreatedAt")
                .HasColumnType("TIMESTAMP")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(e => e.MessageId).HasName("PK_ChatMessage");
            entity.ToTable("ChatMessage");

            entity.Property(e => e.MessageId)
                .HasColumnName("MessageId")
                .UseIdentityColumn();
            entity.Property(e => e.SessionId).HasColumnName("SessionId");
            entity.Property(e => e.Sender).HasColumnName("Sender").HasMaxLength(50);
            entity.Property(e => e.MessageText).HasColumnName("Message").HasMaxLength(2000);
            entity.Property(e => e.SentAt)
                .HasColumnName("SentAt")
                .HasColumnType("TIMESTAMP")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<ChatMessage>()
            .HasOne(m => m.ChatSession)
            .WithMany(s => s.ChatMessages)
            .HasForeignKey(m => m.SessionId)
            .HasConstraintName("FK_ChatMessage_Session")
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserActivityLog>(entity =>
        {
            entity.HasKey(e => e.LogId).HasName("PK_UserActivityLog");
            entity.ToTable("UserActivityLog");

            entity.Property(e => e.LogId)
                .HasColumnName("LogId")
                .UseIdentityColumn();
            entity.Property(e => e.CustomerId).HasColumnName("CustomerID");
            entity.Property(e => e.ActivityType)
                .HasColumnName("ActivityType")
                .HasMaxLength(50);
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.Details)
                .HasColumnName("Details")
                .HasMaxLength(500);
            entity.Property(e => e.Timestamp)
                .HasColumnName("Timestamp")
                .HasColumnType("TIMESTAMP");

            entity.HasOne(d => d.Customer)
                .WithMany()
                .HasForeignKey(d => d.CustomerId)
                .HasConstraintName("FK_UserActivityLog_Customer")
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Product)
                .WithMany()
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK_UserActivityLog_Product")
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<WorkShift>(entity =>
        {
            entity.HasKey(e => e.ShiftId).HasName("PK_WorkShift");

            entity.Property(e => e.ShiftId).UseIdentityColumn();
            entity.Property(e => e.WorkDate).HasColumnType("DATE");
            entity.Property(e => e.ShiftName).HasMaxLength(20);
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue(HrConstants.StatusPending);
            entity.Property(e => e.Note).HasMaxLength(500);

            entity.HasOne(d => d.Admin)
                .WithMany()
                .HasForeignKey(d => d.AdminId)
                .HasPrincipalKey(a => a.AdminId)
                .HasConstraintName("FK_WorkShift_Admin")
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Payroll>(entity =>
        {
            entity.HasKey(e => e.PayrollId).HasName("PK_Payroll");

            entity.Property(e => e.PayrollId).UseIdentityColumn();
            entity.Property(e => e.Month).HasColumnType("NUMBER");
            entity.Property(e => e.Year).HasColumnType("NUMBER");
            entity.Property(e => e.BasicSalaryPerShift).HasColumnType("NUMBER(12, 2)");
            entity.Property(e => e.TotalShifts)
                .HasColumnType("NUMBER")
                .HasDefaultValue(0);
            entity.Property(e => e.NetSalary).HasColumnType("NUMBER(12, 2)");
            entity.Property(e => e.Status).HasMaxLength(50);

            entity.HasOne(d => d.Admin)
                .WithMany()
                .HasForeignKey(d => d.AdminId)
                .HasPrincipalKey(a => a.AdminId)
                .HasConstraintName("FK_Payroll_Admin")
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasIndex(e => new { e.AdminId, e.Month, e.Year })
                .IsUnique()
                .HasDatabaseName("UQ_Payroll_Admin_Period");
        });

        modelBuilder.Entity<GoodsReceipt>(entity =>
        {
            entity.HasKey(e => e.ReceiptId).HasName("PK_GoodsReceipt");

            entity.Property(e => e.ReceiptId).UseIdentityColumn();
            entity.Property(e => e.ImportDate).HasColumnType("DATE");
            entity.Property(e => e.TotalCost).HasColumnType("NUMBER(12, 2)");
            entity.Property(e => e.Note).HasMaxLength(500);

            entity.HasOne(d => d.Admin)
                .WithMany()
                .HasForeignKey(d => d.AdminId)
                .HasPrincipalKey(a => a.AdminId)
                .HasConstraintName("FK_GoodsReceipt_Admin")
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<GoodsReceiptDetail>(entity =>
        {
            entity.HasKey(e => e.DetailId).HasName("PK_GoodsReceiptDetail");

            entity.Property(e => e.DetailId).UseIdentityColumn();
            entity.Property(e => e.Quantity).HasColumnType("NUMBER");
            entity.Property(e => e.ImportPrice).HasColumnType("NUMBER(12, 2)");

            entity.HasOne(d => d.GoodsReceipt)
                .WithMany(r => r.GoodsReceiptDetails)
                .HasForeignKey(d => d.ReceiptId)
                .HasConstraintName("FK_GoodsReceiptDetail_Receipt")
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Product)
                .WithMany()
                .HasForeignKey(d => d.ProductId)
                .HasPrincipalKey(p => p.ProductId)
                .HasConstraintName("FK_GoodsReceiptDetail_Product")
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<ProductPriceHistory>(entity =>
        {
            entity.HasKey(e => e.HistoryId).HasName("PK_ProductPriceHistory");
            entity.ToTable("ProductPriceHistory");

            entity.Property(e => e.HistoryId)
                .HasColumnName("HistoryId")
                .UseIdentityColumn();
            entity.Property(e => e.ProductId).HasColumnName("ProductId");
            entity.Property(e => e.OldPrice)
                .HasColumnName("OldPrice")
                .HasColumnType("NUMBER(12, 2)");
            entity.Property(e => e.NewPrice)
                .HasColumnName("NewPrice")
                .HasColumnType("NUMBER(12, 2)");
            entity.Property(e => e.ChangedAt)
                .HasColumnName("ChangedAt")
                .HasColumnType("TIMESTAMP");
            entity.Property(e => e.AdminId).HasColumnName("AdminId");

            entity.HasOne(d => d.Product)
                .WithMany()
                .HasForeignKey(d => d.ProductId)
                .HasPrincipalKey(p => p.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Admin)
                .WithMany()
                .HasForeignKey(d => d.AdminId)
                .HasPrincipalKey(a => a.AdminId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}