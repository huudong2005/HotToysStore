namespace ToyStore.Models;

/// <summary>
/// Thống kê khuyến mãi & doanh thu thực tế theo tháng (đơn hàng Hoàn thành).
/// </summary>
public class PromotionStatisticViewModel
{
    public int Year { get; set; }

    public int Month { get; set; }

    public decimal TotalVoucherDiscount { get; set; }

    public decimal TotalMembershipDiscount { get; set; }

    public decimal TotalDiscount { get; set; }

    public decimal ActualRevenue { get; set; }
}
