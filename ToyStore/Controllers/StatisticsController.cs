using Microsoft.AspNetCore.Mvc;
using ToyStore.Attributes;
using ToyStore.Domain.Interfaces;
using ToyStore.Models;

namespace ToyStore.Controllers;

/// <summary>
/// Thống kê doanh thu dành cho Admin (Chart.js + Oracle SP).
/// </summary>
[AuthorizeRole("Admin")]
public class StatisticsController : Controller
{
    private readonly IStatisticsRepository _statisticsRepository;
    private readonly ILogger<StatisticsController> _logger;

    public StatisticsController(
        IStatisticsRepository statisticsRepository,
        ILogger<StatisticsController> logger)
    {
        _statisticsRepository = statisticsRepository;
        _logger = logger;
    }

    /// <summary>
    /// Trang báo cáo doanh thu — biểu đồ load qua API GetRevenueData.
    /// </summary>
    [HttpGet]
    public IActionResult Index()
    {
        ViewBag.DefaultYear = DateTime.Now.Year;
        return View();
    }

    /// <summary>
    /// API JSON phục vụ Chart.js.
    /// type: "month" | "quarter" | "year"
    /// year: bắt buộc khi type = month hoặc quarter
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetRevenueData(string type, int? year)
    {
        try
        {
            var normalizedType = (type ?? string.Empty).Trim().ToLowerInvariant();
            List<RevenueViewModel> data;

            switch (normalizedType)
            {
                case "month":
                    if (!year.HasValue || year.Value < 2000 || year.Value > 2100)
                    {
                        return BadRequest(new { success = false, message = "Vui lòng chọn năm hợp lệ (2000–2100)." });
                    }

                    data = await _statisticsRepository.GetRevenueByMonthAsync(year.Value);
                    break;

                case "quarter":
                    if (!year.HasValue || year.Value < 2000 || year.Value > 2100)
                    {
                        return BadRequest(new { success = false, message = "Vui lòng chọn năm hợp lệ (2000–2100)." });
                    }

                    data = await _statisticsRepository.GetRevenueByQuarterAsync(year.Value);
                    break;

                case "year":
                    data = await _statisticsRepository.GetRevenueByYearAsync();
                    break;

                default:
                    return BadRequest(new { success = false, message = "Tham số type không hợp lệ. Dùng: month, quarter, year." });
            }

            return Json(new
            {
                success = true,
                type = normalizedType,
                year = year,
                items = data.Select(x => new
                {
                    label = x.Label,
                    year = x.Year,
                    totalOrders = x.TotalOrders,
                    totalRevenue = x.TotalRevenue
                }),
                summary = new
                {
                    totalOrders = data.Sum(x => x.TotalOrders),
                    totalRevenue = data.Sum(x => x.TotalRevenue)
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetRevenueData failed. type={Type}, year={Year}", type, year);
            return StatusCode(500, new
            {
                success = false,
                message = "Không thể tải dữ liệu thống kê: " + ex.Message
            });
        }
    }

    /// <summary>
    /// API JSON — top sản phẩm bán chạy (Chart.js Doughnut).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetTopProductsData(int topN = 5)
    {
        if (topN < 1 || topN > 100)
        {
            return BadRequest(new { success = false, message = "Tham số topN không hợp lệ (1–100)." });
        }

        try
        {
            var data = await _statisticsRepository.GetTopSellingProductsAsync(topN);

            return Json(new
            {
                success = true,
                topN,
                items = data.Select(x => new
                {
                    productId = x.ProductID,
                    productName = x.ProductName,
                    totalSold = x.TotalSold,
                    revenue = x.Revenue
                }),
                summary = new
                {
                    totalSold = data.Sum(x => x.TotalSold),
                    totalRevenue = data.Sum(x => x.Revenue)
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetTopProductsData failed. topN={TopN}", topN);
            return StatusCode(500, new
            {
                success = false,
                message = "Không thể tải dữ liệu top sản phẩm: " + ex.Message
            });
        }
    }

    /// <summary>
    /// API JSON — top khách hàng chi tiêu nhiều nhất (Chart.js horizontal bar).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetTopCustomersData(int topN = 5)
    {
        if (topN < 1 || topN > 100)
        {
            return BadRequest(new { success = false, message = "Tham số topN không hợp lệ (1–100)." });
        }

        try
        {
            var data = await _statisticsRepository.GetTopCustomersAsync(topN);

            return Json(new
            {
                success = true,
                topN,
                items = data.Select(x => new
                {
                    customerId = x.CustomerID,
                    fullName = x.FullName,
                    email = x.Email,
                    totalOrders = x.TotalOrders,
                    totalSpent = x.TotalSpent
                }),
                summary = new
                {
                    totalOrders = data.Sum(x => x.TotalOrders),
                    totalSpent = data.Sum(x => x.TotalSpent)
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetTopCustomersData failed. topN={TopN}", topN);
            return StatusCode(500, new
            {
                success = false,
                message = "Không thể tải dữ liệu top khách hàng: " + ex.Message
            });
        }
    }
}
