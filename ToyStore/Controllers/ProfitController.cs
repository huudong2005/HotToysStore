using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ToyStore.Attributes;
using ToyStore.Infrastructure.Data;
using ToyStore.Models;

namespace ToyStore.Controllers;

[AuthorizeRole("Admin")]
public class ProfitController : Controller
{
    private const string CompletedOrderStatus = "Hoàn thành";

    private readonly ToyStoreContext _context;

    public ProfitController(ToyStoreContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? month, int? year)
    {
        var filterMonth = month is >= 1 and <= 12 ? month.Value : DateTime.Now.Month;
        var filterYear = year is >= 2000 and <= 2100 ? year.Value : DateTime.Now.Year;

        var periodStart = new DateTime(filterYear, filterMonth, 1);
        var periodEnd = periodStart.AddMonths(1);

        var orderRows = await _context.Orders
            .AsNoTracking()
            .Where(o => o.OrderDate.HasValue
                && o.OrderDate.Value >= periodStart
                && o.OrderDate.Value < periodEnd)
            .Select(o => new { o.Status, o.TotalAmount })
            .ToListAsync();

        var totalRevenue = orderRows
            .Where(o => string.Equals(o.Status, CompletedOrderStatus, StringComparison.Ordinal))
            .Sum(o => o.TotalAmount);

        var receiptRows = await _context.GoodsReceipts
            .AsNoTracking()
            .Where(r => r.ImportDate >= periodStart && r.ImportDate < periodEnd)
            .Select(r => r.TotalCost)
            .ToListAsync();

        var totalCogs = receiptRows.Sum();

        var report = new ProfitReportViewModel
        {
            Month = filterMonth,
            Year = filterYear,
            TotalRevenue = totalRevenue,
            TotalCogs = totalCogs
        };

        return View(report);
    }
}
