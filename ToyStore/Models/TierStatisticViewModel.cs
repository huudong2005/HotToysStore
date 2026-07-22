namespace ToyStore.Models;

/// <summary>
/// Thống kê ưu đãi theo hạng thành viên (VIP).
/// </summary>
public class TierStatisticViewModel
{
    public string TierName { get; set; } = string.Empty;

    public int OrderCount { get; set; }

    public decimal TotalMembershipDiscount { get; set; }
}
