using System;
using System.ComponentModel.DataAnnotations;

namespace ToyStore.Domain.Entities;

public partial class Banner
{
    [Display(Name = "Mã banner")]
    public int BannerId { get; set; }

    [StringLength(255, ErrorMessage = "Tiêu đề không được vượt quá 255 ký tự")]
    [Display(Name = "Tiêu đề")]
    public string? Title { get; set; }

    [Display(Name = "Đường dẫn ảnh")]
    public string ImagePath { get; set; } = null!;

    [Display(Name = "Đang hiển thị")]
    public bool IsActive { get; set; }

    [Display(Name = "Ngày tạo")]
    public DateTime CreatedAt { get; set; }
}
