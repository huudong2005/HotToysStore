using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ToyStore.Domain.Entities;

/// <summary>
/// Hạng thành viên (Membership Tier) - dùng để xếp hạng khách hàng theo số đơn đã hoàn thành
/// và áp dụng mức giảm giá ưu đãi tương ứng.
/// </summary>
public partial class MembershipTier
{
    [Display(Name = "Mã hạng thẻ")]
    public int TierId { get; set; }

    [Required(ErrorMessage = "Tên hạng thẻ là bắt buộc")]
    [StringLength(100, ErrorMessage = "Tên hạng thẻ không được vượt quá 100 ký tự")]
    [Display(Name = "Tên hạng thẻ")]
    public string TierName { get; set; } = null!;

    /// <summary>
    /// Số đơn hàng hoàn thành tối thiểu để đạt hạng thẻ này.
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Số đơn yêu cầu phải lớn hơn hoặc bằng 0")]
    [Display(Name = "Số đơn yêu cầu")]
    public int RequiredOrders { get; set; }

    /// <summary>
    /// Phần trăm giảm giá của hạng thẻ (ví dụ 10 = giảm 10%).
    /// </summary>
    [Range(0, 100, ErrorMessage = "Phần trăm giảm giá phải nằm trong khoảng 0 - 100")]
    [Display(Name = "Phần trăm giảm giá (%)")]
    public decimal DiscountPercent { get; set; }

    // Navigation: các khách hàng đang thuộc hạng thẻ này.
    public virtual ICollection<Customer> Customers { get; set; } = new List<Customer>();
}
