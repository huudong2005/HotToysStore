using System;
using System.ComponentModel.DataAnnotations;

namespace ToyStore.Domain.Entities;

public partial class Promotion
{
    [Display(Name = "Mã khuyến mãi (ID)")]
    public int PromotionId { get; set; }

    [Required(ErrorMessage = "Mã code là bắt buộc")]
    [StringLength(50, ErrorMessage = "Mã code không được vượt quá 50 ký tự")]
    [Display(Name = "Mã code")]
    public string PromotionCode { get; set; } = null!;

    [Required(ErrorMessage = "Tên chương trình là bắt buộc")]
    [StringLength(255, ErrorMessage = "Tên chương trình không được vượt quá 255 ký tự")]
    [Display(Name = "Tên chương trình")]
    public string PromotionName { get; set; } = null!;

    [Required(ErrorMessage = "Loại giảm giá là bắt buộc")]
    [StringLength(50)]
    [Display(Name = "Loại giảm giá")]
    public string DiscountType { get; set; } = null!;

    [Range(0, double.MaxValue, ErrorMessage = "Giá trị giảm phải lớn hơn hoặc bằng 0")]
    [Display(Name = "Giá trị giảm")]
    public decimal DiscountValue { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Giá trị đơn tối thiểu phải lớn hơn hoặc bằng 0")]
    [Display(Name = "Giá trị đơn tối thiểu")]
    public decimal MinOrderValue { get; set; }

    [Required(ErrorMessage = "Ngày bắt đầu là bắt buộc")]
    [DataType(DataType.DateTime)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
    [Display(Name = "Ngày bắt đầu")]
    public DateTime StartDate { get; set; }

    [Required(ErrorMessage = "Ngày kết thúc là bắt buộc")]
    [DataType(DataType.DateTime)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
    [Display(Name = "Ngày kết thúc")]
    public DateTime EndDate { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Giới hạn lượt dùng phải lớn hơn hoặc bằng 0")]
    [Display(Name = "Giới hạn lượt dùng")]
    public int UsageLimit { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Số lượt đã dùng phải lớn hơn hoặc bằng 0")]
    [Display(Name = "Số lượt đã dùng")]
    public int UsedCount { get; set; }

    [Display(Name = "Đang kích hoạt")]
    public bool IsActive { get; set; }
}
