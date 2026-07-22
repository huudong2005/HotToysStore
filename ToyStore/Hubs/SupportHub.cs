using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ToyStore.Domain.Entities;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Hubs;

public class SupportHub : Hub
{
    private const string SupportAgentsGroup = "SupportAgents";
    private const string AdminsGroup = "Admins";
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SupportHub> _logger;

    public SupportHub(IServiceScopeFactory scopeFactory, ILogger<SupportHub> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    private static string RoomName(int sessionId) => sessionId.ToString();

    public async Task JoinSupportAgents()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, SupportAgentsGroup);
    }

    public async Task JoinAdminGroup()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, AdminsGroup);
    }

    public async Task JoinRoom(int sessionId)
    {
        if (sessionId <= 0)
        {
            throw new HubException("SessionId không hợp lệ.");
        }

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ToyStoreContext>();

        var session = await db.ChatSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SessionId == sessionId);

        if (session == null)
        {
            throw new HubException("Phiên hỗ trợ không tồn tại hoặc đã hết hạn.");
        }

        var room = RoomName(sessionId);
        await Groups.AddToGroupAsync(Context.ConnectionId, room);

        try
        {
            var tracked = await db.ChatSessions
                .FirstOrDefaultAsync(s => s.SessionId == sessionId);

            if (tracked != null &&
                string.Equals(tracked.Status, "Pending", StringComparison.OrdinalIgnoreCase))
            {
                tracked.Status = "Active";
                await db.SaveChangesAsync();

                await Clients.Group(room)
                    .SendAsync("SessionStatusChanged", sessionId, "Active");

                await Clients.Group(SupportAgentsGroup)
                    .SendAsync("SessionUpdated", sessionId, "Active");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "JoinRoom: không cập nhật được trạng thái session {SessionId}", sessionId);
        }
    }

    public async Task<int> RequestSupport(string customerName)
    {
        var safeName = string.IsNullOrWhiteSpace(customerName)
            ? "Khách hàng"
            : customerName.Trim();

        if (safeName.Length > 100)
        {
            safeName = safeName[..100];
        }

        ChatSession session;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ToyStoreContext>();

            session = new ChatSession
            {
                CustomerName = safeName,
                Status = "Pending"
            };

            db.ChatSessions.Add(session);
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RequestSupport failed for {CustomerName}", safeName);
            throw new HubException("Không thể tạo phiên hỗ trợ. Vui lòng thử lại sau.");
        }

        var sessionId = session.SessionId;
        var room = RoomName(sessionId);
        var createdAt = session.CreatedAt ?? DateTime.Now;

        await Groups.AddToGroupAsync(Context.ConnectionId, room);

        await Clients.Group(SupportAgentsGroup).SendAsync(
            "NewSupportRequest",
            sessionId,
            safeName,
            "Pending",
            createdAt.ToString("o"));

        await Clients.Group(AdminsGroup).SendAsync(
            "ReceiveAdminNotification",
            "Tin nhắn mới",
            $"Khách hàng {safeName} đang yêu cầu hỗ trợ trực tiếp!",
            "/Support");

        return sessionId;
    }

    public async Task SendMessage(int sessionId, string sender, string message)
    {
        if (sessionId <= 0 || string.IsNullOrWhiteSpace(message))
        {
            throw new HubException("Dữ liệu tin nhắn không hợp lệ.");
        }

        var safeSender = string.IsNullOrWhiteSpace(sender) ? "Unknown" : sender.Trim();
        var safeMessage = message.Trim();

        if (safeMessage.Length > 2000)
        {
            safeMessage = safeMessage[..2000];
        }

        if (safeSender.Length > 50)
        {
            safeSender = safeSender[..50];
        }

        var sentAt = DateTime.Now;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ToyStoreContext>();

            var sessionExists = await db.ChatSessions
                .AsNoTracking()
                .AnyAsync(s => s.SessionId == sessionId
                    && s.Status != "Closed");

            if (!sessionExists)
            {
                throw new HubException("Phiên hỗ trợ không tồn tại hoặc đã đóng.");
            }

            var chatMessage = new ChatMessage
            {
                SessionId = sessionId,
                Sender = safeSender,
                MessageText = safeMessage,
                SentAt = sentAt
            };

            db.ChatMessages.Add(chatMessage);
            await db.SaveChangesAsync();
        }
        catch (HubException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SendMessage failed for session {SessionId}", sessionId);
            throw new HubException("Không thể gửi tin nhắn. Vui lòng thử lại.");
        }

        await Clients.Group(RoomName(sessionId)).SendAsync(
            "ReceiveMessage",
            sessionId,
            safeSender,
            safeMessage,
            sentAt.ToString("o"));
    }

    public async Task DisconnectSupport(int sessionId, string initiator)
    {
        if (sessionId <= 0)
        {
            throw new HubException("SessionId không hợp lệ.");
        }

        var safeInitiator = string.IsNullOrWhiteSpace(initiator) ? "Customer" : initiator.Trim();
        if (safeInitiator.Length > 50)
        {
            safeInitiator = safeInitiator[..50];
        }

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ToyStoreContext>();

        var session = await db.ChatSessions
            .FirstOrDefaultAsync(s => s.SessionId == sessionId);

        if (session == null)
        {
            throw new HubException("Phiên hỗ trợ không tồn tại.");
        }

        if (string.Equals(session.Status, "Closed", StringComparison.OrdinalIgnoreCase))
        {
            await Clients.Caller.SendAsync("SessionStatusChanged", sessionId, "Closed", safeInitiator);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, RoomName(sessionId));
            return;
        }

        session.Status = "Closed";

        try
        {
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DisconnectSupport failed for session {SessionId}", sessionId);
            throw new HubException("Không thể ngắt kết nối. Vui lòng thử lại.");
        }

        var room = RoomName(sessionId);

        await Clients.Group(room)
            .SendAsync("SessionStatusChanged", sessionId, "Closed", safeInitiator);

        await Clients.Group(SupportAgentsGroup)
            .SendAsync("SessionUpdated", sessionId, "Closed", safeInitiator);

        await Clients.Group(AdminsGroup)
            .SendAsync("SessionUpdated", sessionId, "Closed", safeInitiator);

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, room);
    }
}
