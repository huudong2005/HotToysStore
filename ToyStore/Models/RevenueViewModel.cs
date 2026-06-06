namespace ToyStore.Models;

/// <summary>
/// Dữ liệu doanh thu trả về từ các SP thống kê Oracle (tháng / quý / năm).
/// </summary>
public class RevenueViewModel
{
    /// <summary>
    /// Nhãn hiển thị trên biểu đồ (vd: "Tháng 3/2026", "Quý 2/2026", "2025").
    /// </summary>
    public string Label { get; set; } = string.Empty;

    public int Year { get; set; }

    public int TotalOrders { get; set; }

    public decimal TotalRevenue { get; set; }
}
