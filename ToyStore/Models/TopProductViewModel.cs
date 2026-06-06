namespace ToyStore.Models;

/// <summary>
/// Dữ liệu sản phẩm bán chạy trả về từ SP_TOP_SELLING_PRODUCTS (Oracle).
/// </summary>
public class TopProductViewModel
{
    public int ProductID { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public int TotalSold { get; set; }

    public decimal Revenue { get; set; }
}
