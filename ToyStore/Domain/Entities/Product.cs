using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ToyStore.Domain.Entities;

[Table("Product")]
public partial class Product
{
    [Key]
    [Column("ProductID")]
    [Display(Name = "Mã sản phẩm")]
    public int ProductId { get; set; }

    [Column("CategoryID")]
    [Required(ErrorMessage = "Danh mục là bắt buộc")]
    [Display(Name = "Danh mục")]
    public int CategoryId { get; set; }

    [Column("ProductName")]
    [Required(ErrorMessage = "Tên sản phẩm là bắt buộc")]
    [StringLength(200, ErrorMessage = "Tên sản phẩm không được vượt quá 200 ký tự")]
    [Display(Name = "Tên sản phẩm")]
    public string ProductName { get; set; } = null!;

    [Column("Description")]
    [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự")]
    [Display(Name = "Mô tả")]
    public string? Description { get; set; }

    [Column("Price")]
    [Required(ErrorMessage = "Giá bán là bắt buộc")]
    [Range(0, double.MaxValue, ErrorMessage = "Giá bán phải lớn hơn hoặc bằng 0")]
    [Display(Name = "Giá bán")]
    public decimal Price { get; set; }

    [Column("Stock")]
    [Required(ErrorMessage = "Số lượng tồn kho là bắt buộc")]
    [Range(0, int.MaxValue, ErrorMessage = "Số lượng tồn kho phải lớn hơn hoặc bằng 0")]
    [Display(Name = "Số lượng tồn kho")]
    public int Stock { get; set; }

    [Column("ImageURL")]
    [StringLength(255, ErrorMessage = "URL hình ảnh không được vượt quá 255 ký tự")]
    [Display(Name = "URL hình ảnh")]
    public string? ImageUrl { get; set; }

    [Column("Status")]
    [Display(Name = "Trạng thái")]
    public bool? Status { get; set; }

    // Navigation Properties - Chuyển sang nullable (?) để tránh lỗi 400 khi Bind dữ liệu từ Form
    public virtual ICollection<CartItem>? CartItems { get; set; } = new List<CartItem>();

    public virtual Category? Category { get; set; }

    public virtual ICollection<OrderDetail>? OrderDetails { get; set; } = new List<OrderDetail>();

    public virtual ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();
}
