namespace ToyStore.Models;

/// <summary>
/// Dữ liệu khách hàng chi tiêu nhiều nhất trả về từ SP_TOP_CUSTOMERS (Oracle).
/// </summary>
public class TopCustomerViewModel
{
    public int CustomerID { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public int TotalOrders { get; set; }

    public decimal TotalSpent { get; set; }
}
