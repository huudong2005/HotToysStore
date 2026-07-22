namespace ToyStore.Models;

/// <summary>
/// Thống kê hiệu quả theo mã khuyến mãi (voucher).
/// </summary>
public class VoucherStatisticViewModel
{
    public string PromotionCode { get; set; } = string.Empty;

    public int UsageCount { get; set; }

    public decimal TotalDiscount { get; set; }
}
