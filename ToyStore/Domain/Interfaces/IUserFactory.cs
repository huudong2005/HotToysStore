using ToyStore.Domain.Entities;
using ToyStore.Models;

namespace ToyStore.Domain.Interfaces;

/// <summary>
/// Interface định nghĩa Factory Method Pattern cho việc tạo các đối tượng User
/// </summary>
public interface IUserFactory
{
    /// <summary>
    /// Tạo Customer với Role/Claims mặc định
    /// </summary>
    /// <param name="model">Thông tin đăng ký</param>
    /// <param name="passwordHash">Mật khẩu đã được hash</param>
    /// <returns>Customer entity với các thuộc tính mặc định</returns>
    Customer CreateCustomer(RegisterViewModel model, string passwordHash);

    /// <summary>
    /// Tạo Staff (Admin với Role = "Staff") với Role/Claims mặc định
    /// </summary>
    /// <param name="model">Thông tin tạo staff</param>
    /// <param name="passwordHash">Mật khẩu đã được hash</param>
    /// <returns>Admin entity với Role = "Staff"</returns>
    Admin CreateStaff(CreateStaffViewModel model, string passwordHash);

    /// <summary>
    /// Tạo Admin (Admin với Role = "Admin") với Role/Claims mặc định
    /// </summary>
    /// <param name="model">Thông tin tạo admin</param>
    /// <param name="passwordHash">Mật khẩu đã được hash</param>
    /// <returns>Admin entity với Role = "Admin"</returns>
    Admin CreateAdmin(CreateStaffViewModel model, string passwordHash);

    /// <summary>
    /// Tạo Admin với username và password (dùng cho tạo admin mặc định)
    /// </summary>
    /// <param name="username">Username</param>
    /// <param name="passwordHash">Mật khẩu đã được hash</param>
    /// <param name="fullName">Họ tên</param>
    /// <param name="role">Role (Admin hoặc Staff)</param>
    /// <returns>Admin entity</returns>
    Admin CreateAdmin(string username, string passwordHash, string? fullName = null, string role = "Admin");
}
