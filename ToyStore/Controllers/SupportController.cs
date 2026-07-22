using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ToyStore.Attributes;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Controllers;

[AuthorizeRole("Admin", "Staff")]
public class SupportController : Controller
{
    private readonly ToyStoreContext _context;

    public SupportController(ToyStoreContext context)
    {
        _context = context;
    }

    // GET: Support — Trang trò chuyện hỗ trợ cho Admin/Staff
    public async Task<IActionResult> Index()
    {
        var sessions = await _context.ChatSessions
            .AsNoTracking()
            .Where(s => s.Status == "Pending" || s.Status == "Active")
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

        return View(sessions);
    }

    // GET: Support/GetSessions — JSON danh sách phiên (Pending/Active)
    [HttpGet]
    public async Task<IActionResult> GetSessions()
    {
        try
        {
            var sessions = await _context.ChatSessions
                .AsNoTracking()
                .Where(s => s.Status == "Pending" || s.Status == "Active")
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => new
                {
                    s.SessionId,
                    s.CustomerName,
                    s.Status,
                    CreatedAt = s.CreatedAt.HasValue ? s.CreatedAt.Value.ToString("o") : null
                })
                .ToListAsync();

            return Json(new { success = true, sessions });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    // GET: Support/GetMessages/{sessionId} — Lịch sử tin nhắn của một phiên
    [HttpGet]
    public async Task<IActionResult> GetMessages(int sessionId)
    {
        if (sessionId <= 0)
        {
            return Json(new { success = false, message = "SessionId không hợp lệ." });
        }

        try
        {
            var session = await _context.ChatSessions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.SessionId == sessionId);

            if (session == null)
            {
                return Json(new { success = false, message = "Không tìm thấy phiên hỗ trợ." });
            }

            var messages = await _context.ChatMessages
                .AsNoTracking()
                .Where(m => m.SessionId == sessionId)
                .OrderBy(m => m.SentAt)
                .Select(m => new
                {
                    m.MessageId,
                    m.SessionId,
                    m.Sender,
                    Message = m.MessageText,
                    SentAt = m.SentAt.HasValue ? m.SentAt.Value.ToString("o") : null
                })
                .ToListAsync();

            return Json(new
            {
                success = true,
                session = new
                {
                    session.SessionId,
                    session.CustomerName,
                    session.Status,
                    CreatedAt = session.CreatedAt.HasValue ? session.CreatedAt.Value.ToString("o") : null
                },
                messages
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }
}
