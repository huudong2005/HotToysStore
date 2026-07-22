using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ToyStore.Attributes;
using ToyStore.Domain.Entities;
using ToyStore.Helpers;
using ToyStore.Infrastructure.Data;
using ToyStore.Models;

namespace ToyStore.Controllers;

[AuthorizeRole("Admin")]
public class WorkShiftController : Controller
{
    private readonly ToyStoreContext _context;
    private readonly ILogger<WorkShiftController> _logger;

    public WorkShiftController(ToyStoreContext context, ILogger<WorkShiftController> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IActionResult> Index(int? adminId, int? month, int? year, string? status)
    {
        var filterMonth = month is >= 1 and <= 12 ? month.Value : DateTime.Now.Month;
        var filterYear = year is >= 2000 and <= 2100 ? year.Value : DateTime.Now.Year;
        var periodStart = new DateTime(filterYear, filterMonth, 1);
        var periodEnd = periodStart.AddMonths(1);

        var query = _context.WorkShifts
            .AsNoTracking()
            .Include(w => w.Admin)
            .Where(w => w.WorkDate >= periodStart && w.WorkDate < periodEnd);

        if (adminId is > 0)
        {
            query = query.Where(w => w.AdminId == adminId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(w => w.Status == status);
        }

        var shifts = await query
            .OrderByDescending(w => w.WorkDate)
            .ThenBy(w => w.ShiftName)
            .ToListAsync();

        await PopulateStaffDropdownAsync(adminId);
        ViewBag.Month = filterMonth;
        ViewBag.Year = filterYear;
        ViewBag.Status = status;
        ViewBag.StatusOptions = HrConstants.AttendanceStatuses;

        return View(shifts);
    }

    public async Task<IActionResult> Create()
    {
        await PopulateStaffDropdownAsync(null);
        return View(new CreateWorkShiftViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateWorkShiftViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateStaffDropdownAsync(model.AdminId);
            return View(model);
        }

        if (!HrConstants.ShiftNames.Contains(model.ShiftName))
        {
            ModelState.AddModelError(nameof(model.ShiftName), "Ca làm việc không hợp lệ.");
            await PopulateStaffDropdownAsync(model.AdminId);
            return View(model);
        }

        var staff = await _context.Admins
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.AdminId == model.AdminId && a.Role != "Admin");

        if (staff == null)
        {
            ModelState.AddModelError(nameof(model.AdminId), "Nhân viên không tồn tại hoặc không được phép phân ca.");
            await PopulateStaffDropdownAsync(model.AdminId);
            return View(model);
        }

        var workDate = model.WorkDate.Date;

        var duplicate = await _context.WorkShifts.AnyAsync(w =>
            w.AdminId == model.AdminId
            && w.WorkDate == workDate
            && w.ShiftName == model.ShiftName);

        if (duplicate)
        {
            ModelState.AddModelError(string.Empty,
                $"Nhân viên đã có ca {model.ShiftName} vào ngày {workDate:dd/MM/yyyy}.");
            await PopulateStaffDropdownAsync(model.AdminId);
            return View(model);
        }

        try
        {
            _context.WorkShifts.Add(new WorkShift
            {
                AdminId = model.AdminId,
                WorkDate = workDate,
                ShiftName = model.ShiftName,
                Status = HrConstants.StatusPending
            });

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Phân ca làm việc thành công.";
            return RedirectToAction(nameof(Index), new
            {
                adminId = model.AdminId,
                month = workDate.Month,
                year = workDate.Year
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tạo ca làm việc cho AdminId={AdminId}", model.AdminId);
            ModelState.AddModelError(string.Empty, "Không thể lưu ca làm việc. Vui lòng thử lại.");
            await PopulateStaffDropdownAsync(model.AdminId);
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int shiftId, string status, int? month, int? year, int? adminId, string? filterStatus)
    {
        if (!HrConstants.UpdatableAttendanceStatuses.Contains(status))
        {
            TempData["ErrorMessage"] = "Trạng thái chấm công không hợp lệ.";
            return RedirectToFilter(month, year, adminId, filterStatus);
        }

        var shift = await _context.WorkShifts.FirstOrDefaultAsync(w => w.ShiftId == shiftId);
        if (shift == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy ca làm việc.";
            return RedirectToFilter(month, year, adminId, filterStatus);
        }

        try
        {
            shift.Status = status;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã cập nhật chấm công ca #{shiftId} thành \"{status}\".";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi chấm công ca ShiftId={ShiftId}", shiftId);
            TempData["ErrorMessage"] = "Không thể cập nhật chấm công. Vui lòng thử lại.";
        }

        return RedirectToFilter(month ?? shift.WorkDate.Month, year ?? shift.WorkDate.Year, adminId, filterStatus);
    }

    private RedirectToActionResult RedirectToFilter(int? month, int? year, int? adminId, string? status)
    {
        return RedirectToAction(nameof(Index), new { adminId, month, year, status });
    }

    private async Task PopulateStaffDropdownAsync(int? selectedAdminId)
    {
        var staffList = await _context.Admins
            .AsNoTracking()
            .Where(a => a.Role != "Admin")
            .OrderBy(a => a.FullName)
            .ThenBy(a => a.Username)
            .ToListAsync();

        ViewBag.StaffList = new SelectList(staffList, nameof(Admin.AdminId), nameof(Admin.FullName), selectedAdminId);
        ViewBag.ShiftNames = HrConstants.ShiftNames;
    }
}
