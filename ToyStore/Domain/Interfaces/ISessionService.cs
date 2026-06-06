using ToyStore.Models;

namespace ToyStore.Domain.Interfaces;

/// <summary>
/// Interface định nghĩa service xử lý Session-based Authentication
/// Tuân thủ Single Responsibility Principle (SOLID)
/// </summary>
public interface ISessionService
{
    /// <summary>
    /// Lưu thông tin user vào session sau khi đăng nhập thành công
    /// </summary>
    /// <param name="context">HttpContext</param>
    /// <param name="userSession">Thông tin user session</param>
    void SetUserSession(HttpContext context, UserSession userSession);

    /// <summary>
    /// Lấy thông tin user từ session
    /// </summary>
    /// <param name="context">HttpContext</param>
    /// <returns>UserSession nếu đã đăng nhập, null nếu chưa đăng nhập</returns>
    UserSession? GetUserSession(HttpContext context);

    /// <summary>
    /// Xóa toàn bộ session (đăng xuất)
    /// </summary>
    /// <param name="context">HttpContext</param>
    void ClearSession(HttpContext context);

    /// <summary>
    /// Kiểm tra user đã đăng nhập chưa
    /// </summary>
    /// <param name="context">HttpContext</param>
    /// <returns>True nếu đã đăng nhập, False nếu chưa</returns>
    bool IsAuthenticated(HttpContext context);

    /// <summary>
    /// Kiểm tra user có phải Admin không
    /// </summary>
    /// <param name="context">HttpContext</param>
    /// <returns>True nếu là Admin</returns>
    bool IsAdmin(HttpContext context);

    /// <summary>
    /// Kiểm tra user có phải Staff hoặc Admin không
    /// </summary>
    /// <param name="context">HttpContext</param>
    /// <returns>True nếu là Staff hoặc Admin</returns>
    bool IsStaff(HttpContext context);

    /// <summary>
    /// Kiểm tra user có phải Customer không
    /// </summary>
    /// <param name="context">HttpContext</param>
    /// <returns>True nếu là Customer</returns>
    bool IsCustomer(HttpContext context);

    /// <summary>
    /// Kiểm tra user có Role cụ thể không
    /// </summary>
    /// <param name="context">HttpContext</param>
    /// <param name="role">Role cần kiểm tra</param>
    /// <returns>True nếu có Role</returns>
    bool HasRole(HttpContext context, string role);

    /// <summary>
    /// Kiểm tra user có bất kỳ Role nào trong danh sách không
    /// </summary>
    /// <param name="context">HttpContext</param>
    /// <param name="roles">Danh sách roles</param>
    /// <returns>True nếu có ít nhất một Role</returns>
    bool HasAnyRole(HttpContext context, params string[] roles);

    /// <summary>
    /// Lấy UserId từ session
    /// </summary>
    /// <param name="context">HttpContext</param>
    /// <returns>UserId nếu đã đăng nhập, 0 nếu chưa</returns>
    int GetUserId(HttpContext context);
}
