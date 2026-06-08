using System;
using System.Collections.Generic;

namespace ToyStore.Domain.Entities;

public partial class Customer
{
    public int CustomerId { get; set; }

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? Phone { get; set; }

    public string? Address { get; set; }

    public string PasswordHash { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    /// <summary>
    /// Tổng số đơn hàng đã hoàn thành (giao thành công) của khách - dùng để xét thăng hạng.
    /// </summary>
    public int TotalCompletedOrders { get; set; } = 0;

    /// <summary>
    /// Hạng thẻ thành viên hiện tại (FK tới MembershipTier). Null nếu khách chưa đạt hạng nào.
    /// </summary>
    public int? TierId { get; set; }

    /// <summary>
    /// Tài khoản bị khóa bởi Admin (khách vi phạm) — không cho đăng nhập khi true.
    /// </summary>
    public bool IsLocked { get; set; }

    public virtual MembershipTier? Tier { get; set; }

    public virtual ICollection<Cart> Carts { get; set; } = new List<Cart>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
