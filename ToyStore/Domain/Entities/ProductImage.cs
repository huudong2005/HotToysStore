using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ToyStore.Domain.Entities;

[Table("ProductImage")]
public partial class ProductImage
{
    [Key]
    [Column("ImageId")]
    [Display(Name = "Mã ảnh")]
    public int ImageId { get; set; }

    [Column("ProductId")]
    [Display(Name = "Mã sản phẩm")]
    public int ProductId { get; set; }

    [Column("ImageUrl")]
    [StringLength(255, ErrorMessage = "URL hình ảnh không được vượt quá 255 ký tự")]
    [Display(Name = "URL hình ảnh")]
    public string ImageUrl { get; set; } = null!;

    [Column("DisplayOrder")]
    [Display(Name = "Thứ tự hiển thị")]
    public int DisplayOrder { get; set; }

    public virtual Product? Product { get; set; }
}
