using ToyStore.Models;

namespace ToyStore.Domain.Interfaces;

/// <summary>
/// Truy vấn thống kê doanh thu qua EF Core (Oracle).
/// </summary>
public interface IStatisticsRepository
{
    /// <summary>Gọi SP_REVENUEBYMONTH — doanh thu theo từng tháng trong năm.</summary>
    Task<List<RevenueViewModel>> GetRevenueByMonthAsync(int year);

    /// <summary>Gọi SP_REVENUE_BY_QUARTER — doanh thu theo từng quý trong năm.</summary>
    Task<List<RevenueViewModel>> GetRevenueByQuarterAsync(int year);

    /// <summary>Gọi SP_REVENUE_BY_YEAR — doanh thu theo từng năm.</summary>
    Task<List<RevenueViewModel>> GetRevenueByYearAsync();

    /// <summary>Gọi SP_TOP_SELLING_PRODUCTS — top sản phẩm bán chạy.</summary>
    Task<List<TopProductViewModel>> GetTopSellingProductsAsync(int topN);

    /// <summary>Gọi SP_TOP_CUSTOMERS — top khách hàng chi tiêu nhiều nhất.</summary>
    Task<List<TopCustomerViewModel>> GetTopCustomersAsync(int topN);
}
