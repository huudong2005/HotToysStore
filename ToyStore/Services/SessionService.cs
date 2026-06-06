using ToyStore.Domain.Interfaces;
using ToyStore.Models;

namespace ToyStore.Services;

/// <summary>
/// Service xử lý Session-based Authentication
/// Tuân thủ Single Responsibility Principle (SOLID)
/// </summary>
public class SessionService : ISessionService
{
    private const string USER_ID_KEY = "UserId";
    private const string USERNAME_KEY = "Username";
    private const string EMAIL_KEY = "Email";
    private const string FULL_NAME_KEY = "FullName";
    private const string USER_TYPE_KEY = "UserType";
    private const string ROLE_KEY = "Role";
    private const string IS_AUTHENTICATED_KEY = "IsAuthenticated";
    private const string SHOW_WELCOME_TOAST_KEY = "ShowWelcomeToast";

    /// <summary>
    /// Lưu thông tin user vào session sau khi đăng nhập thành công
    /// </summary>
    public void SetUserSession(HttpContext context, UserSession userSession)
    {
        if (context == null || userSession == null)
            return;

        context.Session.SetString(USER_ID_KEY, userSession.UserId.ToString());
        context.Session.SetString(USERNAME_KEY, userSession.Username);
        context.Session.SetString(EMAIL_KEY, userSession.Email);
        context.Session.SetString(FULL_NAME_KEY, userSession.FullName);
        context.Session.SetString(USER_TYPE_KEY, userSession.UserType);
        context.Session.SetString(ROLE_KEY, userSession.Role);
        context.Session.SetString(IS_AUTHENTICATED_KEY, userSession.IsAuthenticated.ToString());
        
        // Set flag to show welcome toast after login
        context.Session.SetString(SHOW_WELCOME_TOAST_KEY, "true");
    }

    /// <summary>
    /// Lấy thông tin user từ session
    /// </summary>
    public UserSession? GetUserSession(HttpContext context)
    {
        if (context == null || !IsAuthenticated(context))
            return null;

        var userIdStr = context.Session.GetString(USER_ID_KEY);
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            return null;

        return new UserSession
        {
            UserId = userId,
            Username = context.Session.GetString(USERNAME_KEY) ?? "",
            Email = context.Session.GetString(EMAIL_KEY) ?? "",
            FullName = context.Session.GetString(FULL_NAME_KEY) ?? "",
            UserType = context.Session.GetString(USER_TYPE_KEY) ?? "",
            Role = context.Session.GetString(ROLE_KEY) ?? "",
            IsAuthenticated = true
        };
    }

    /// <summary>
    /// Xóa toàn bộ session (đăng xuất)
    /// </summary>
    public void ClearSession(HttpContext context)
    {
        context?.Session.Clear();
    }

    /// <summary>
    /// Kiểm tra user đã đăng nhập chưa
    /// </summary>
    public bool IsAuthenticated(HttpContext context)
    {
        return context != null && 
               context.Session.GetString(IS_AUTHENTICATED_KEY) == "True";
    }

    /// <summary>
    /// Kiểm tra user có phải Admin không
    /// </summary>
    public bool IsAdmin(HttpContext context)
    {
        return IsAuthenticated(context) && 
               context.Session.GetString(USER_TYPE_KEY) == "Admin";
    }

    /// <summary>
    /// Kiểm tra user có phải Staff hoặc Admin không
    /// </summary>
    public bool IsStaff(HttpContext context)
    {
        if (!IsAuthenticated(context))
            return false;

        var userType = context.Session.GetString(USER_TYPE_KEY);
        return userType == "Staff" || userType == "Admin";
    }

    /// <summary>
    /// Kiểm tra user có phải Customer không
    /// </summary>
    public bool IsCustomer(HttpContext context)
    {
        return IsAuthenticated(context) && 
               context.Session.GetString(USER_TYPE_KEY) == "Customer";
    }

    /// <summary>
    /// Kiểm tra user có Role cụ thể không
    /// </summary>
    public bool HasRole(HttpContext context, string role)
    {
        return IsAuthenticated(context) && 
               context.Session.GetString(ROLE_KEY) == role;
    }

    /// <summary>
    /// Kiểm tra user có bất kỳ Role nào trong danh sách không
    /// </summary>
    public bool HasAnyRole(HttpContext context, params string[] roles)
    {
        if (!IsAuthenticated(context))
            return false;

        var userRole = context.Session.GetString(ROLE_KEY);
        return !string.IsNullOrEmpty(userRole) && roles.Contains(userRole);
    }

    /// <summary>
    /// Lấy UserId từ session
    /// </summary>
    public int GetUserId(HttpContext context)
    {
        if (context == null)
            return 0;

        var userIdStr = context.Session.GetString(USER_ID_KEY);
        if (int.TryParse(userIdStr, out int userId))
        {
            return userId;
        }

        return 0;
    }
}
