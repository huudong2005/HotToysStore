using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ToyStore.Attributes;
using ToyStore.Infrastructure.Data;
using ToyStore.Models;

namespace ToyStore.Controllers;

[AuthorizeRole("Admin")]
public class ActivityLogController : Controller
{
    private const int PageSize = 20;

    private readonly ToyStoreContext _context;

    public ActivityLogController(ToyStoreContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string searchEmail, string activityType, int page = 1)
    {
        page = page < 1 ? 1 : page;

        var query = _context.UserActivityLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(searchEmail))
        {
            var email = searchEmail.Trim();
            query = query.Where(l => l.Customer.Email.Contains(email));
        }

        if (!string.IsNullOrWhiteSpace(activityType))
        {
            query = query.Where(l => l.ActivityType == activityType);
        }

        var totalCount = await query.CountAsync();
        var totalPages = totalCount == 0 ? 1 : (int)Math.Ceiling(totalCount / (double)PageSize);

        if (page > totalPages)
        {
            page = totalPages;
        }

        var logs = await query
            .OrderByDescending(l => l.Timestamp)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .Include(l => l.Customer)
            .Include(l => l.Product)
            .ToListAsync();

        var viewModel = new ActivityLogIndexViewModel
        {
            Logs = logs,
            CurrentPage = page,
            TotalPages = totalPages,
            TotalCount = totalCount,
            PageSize = PageSize,
            SearchEmail = searchEmail,
            ActivityType = activityType
        };

        return View(viewModel);
    }
}
