using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ToyStore.Attributes;
using ToyStore.Domain.Entities;
using ToyStore.Helpers;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Controllers;

[AuthorizeRole("Admin")]
public class PayrollController : Controller
{
    private readonly ToyStoreContext _context;
    private readonly ILogger<PayrollController> _logger;

    public PayrollController(ToyStoreContext context, ILogger<PayrollController> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IActionResult> Index(int? month, int? year)
    {
        var filterMonth = month is >= 1 and <= 12 ? month.Value : DateTime.Now.Month;
        var filterYear = year is >= 2000 and <= 2100 ? year.Value : DateTime.Now.Year;

        var payrolls = await _context.Payrolls
            .AsNoTracking()
            .Include(p => p.Admin)
            .Where(p => p.Month == filterMonth && p.Year == filterYear)
            .OrderBy(p => p.Admin!.FullName)
            .ThenBy(p => p.Admin!.Username)
            .ToListAsync();

        ViewBag.Month = filterMonth;
        ViewBag.Year = filterYear;
        ViewBag.TotalNetSalary = payrolls.Sum(p => p.NetSalary);
        ViewBag.TotalShifts = payrolls.Sum(p => p.TotalShifts);

        return View(payrolls);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Calculate(int month, int year)
    {
        if (month is < 1 or > 12 || year is < 2000 or > 2100)
        {
            TempData["ErrorMessage"] = "Tháng/năm không hợp lệ.";
            return RedirectToAction(nameof(Index));
        }

        var periodStart = new DateTime(year, month, 1);
        var periodEnd = periodStart.AddMonths(1);

        try
        {
            var staffList = await _context.Admins
                .AsNoTracking()
                .Where(a => a.Role != "Admin")
                .OrderBy(a => a.AdminId)
                .ToListAsync();

            if (staffList.Count == 0)
            {
                TempData["ErrorMessage"] = "Không có nhân viên để tính lương.";
                return RedirectToAction(nameof(Index), new { month, year });
            }

            var shiftCounts = await _context.WorkShifts
                .AsNoTracking()
                .Where(w =>
                    w.Status == HrConstants.StatusCompleted
                    && w.WorkDate >= periodStart
                    && w.WorkDate < periodEnd)
                .GroupBy(w => w.AdminId)
                .Select(g => new { AdminId = g.Key, Count = g.Count() })
                .ToListAsync();

            var countLookup = shiftCounts.ToDictionary(x => x.AdminId, x => x.Count);

            var existingPayrolls = await _context.Payrolls
                .Where(p => p.Month == month && p.Year == year)
                .ToListAsync();

            var payrollLookup = existingPayrolls.ToDictionary(p => p.AdminId);
            var created = 0;
            var updated = 0;

            foreach (var staff in staffList)
            {
                countLookup.TryGetValue(staff.AdminId, out var completedCount);
                var basicSalary = HrConstants.SalaryPerCompletedShift;
                var netSalary = completedCount * basicSalary;

                if (payrollLookup.TryGetValue(staff.AdminId, out var existing))
                {
                    existing.TotalShifts = completedCount;
                    existing.BasicSalaryPerShift = basicSalary;
                    existing.NetSalary = netSalary;
                    existing.Status = HrConstants.PayrollStatusFinalized;
                    updated++;
                }
                else
                {
                    _context.Payrolls.Add(new Payroll
                    {
                        AdminId = staff.AdminId,
                        Month = month,
                        Year = year,
                        TotalShifts = completedCount,
                        BasicSalaryPerShift = basicSalary,
                        NetSalary = netSalary,
                        Status = HrConstants.PayrollStatusFinalized
                    });
                    created++;
                }
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"Chốt lương tháng {month}/{year} thành công: {created} bản ghi mới, {updated} bản ghi cập nhật.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi chốt lương tháng {Month}/{Year}", month, year);
            TempData["ErrorMessage"] = "Không thể chốt lương. Vui lòng kiểm tra dữ liệu ca làm việc và thử lại.";
        }

        return RedirectToAction(nameof(Index), new { month, year });
    }
}
