using Microsoft.AspNetCore.Mvc;
using ToyStore.Attributes;
using ToyStore.Domain.Interfaces;

namespace ToyStore.Controllers;

/// <summary>
/// Thống kê khuyến mãi & doanh thu thực tế dành cho Admin.
/// </summary>
[AuthorizeRole("Admin")]
public class RevenueController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    public RevenueController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Báo cáo tiền giảm (Voucher + Hạng thành viên) và doanh thu thực thu theo tháng.
    /// Bộ lọc lai: ưu tiên khoảng ngày (startDate + endDate), nếu không có thì lọc theo năm.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index(int? year, DateTime? startDate, DateTime? endDate)
    {
        DateTime rangeStart;
        DateTime rangeEnd;
        string filterMode;
        int? selectedYear;

        if (startDate.HasValue && endDate.HasValue)
        {
            filterMode = "DateRange";
            rangeStart = startDate.Value.Date;
            rangeEnd = endDate.Value.Date.AddHours(23).AddMinutes(59).AddSeconds(59);
            selectedYear = year;
        }
        else
        {
            filterMode = "Year";
            selectedYear = year ?? DateTime.Now.Year;
            rangeStart = new DateTime(selectedYear.Value, 1, 1);
            rangeEnd = new DateTime(selectedYear.Value, 12, 31, 23, 59, 59);
            startDate = null;
            endDate = null;
        }

        var statistics = await _unitOfWork.Orders.GetPromotionStatisticsAsync(rangeStart, rangeEnd);

        ViewBag.FilterMode = filterMode;
        ViewBag.SelectedYear = selectedYear;
        ViewBag.StartDate = startDate;
        ViewBag.EndDate = endDate;
        ViewBag.RangeStart = rangeStart;
        ViewBag.RangeEnd = rangeEnd;

        return View(statistics);
    }
}
