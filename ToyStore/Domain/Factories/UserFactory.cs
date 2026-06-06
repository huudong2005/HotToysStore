using ToyStore.Domain.Entities;
using ToyStore.Domain.Interfaces;
using ToyStore.Models;

namespace ToyStore.Domain.Factories;

/// <summary>
/// Factory Method Pattern: Tạo các đối tượng User với Role/Claims mặc định
/// </summary>
public class UserFactory : IUserFactory
{
    // Constants cho Roles/Claims
    private const string CUSTOMER_ROLE = "Customer";
    private const string STAFF_ROLE = "Staff";
    private const string ADMIN_ROLE = "Admin";

    /// <summary>
    /// Tạo Customer với Role/Claims mặc định
    /// </summary>
    public Customer CreateCustomer(RegisterViewModel model, string passwordHash)
    {
        return new Customer
        {
            FullName = model.FullName,
            Email = model.Email,
            PasswordHash = passwordHash,
            Phone = model.Phone,
            Address = model.Address,
            CreatedAt = DateTime.Now
            // Role mặc định: Customer (không lưu trong Customer entity, 
            // được xác định bởi UserType trong UserSession)
        };
    }

    /// <summary>
    /// Tạo Staff (Admin với Role = "Staff") với Role/Claims mặc định
    /// </summary>
    public Admin CreateStaff(CreateStaffViewModel model, string passwordHash)
    {
        return new Admin
        {
            Username = model.Username,
            PasswordHash = passwordHash,
            FullName = model.FullName,
            Role = STAFF_ROLE // Role mặc định cho Staff
        };
    }

    /// <summary>
    /// Tạo Admin (Admin với Role = "Admin") với Role/Claims mặc định
    /// </summary>
    public Admin CreateAdmin(CreateStaffViewModel model, string passwordHash)
    {
        return new Admin
        {
            Username = model.Username,
            PasswordHash = passwordHash,
            FullName = model.FullName,
            Role = ADMIN_ROLE // Role mặc định cho Admin
        };
    }

    /// <summary>
    /// Tạo Admin với username và password (dùng cho tạo admin mặc định)
    /// </summary>
    public Admin CreateAdmin(string username, string passwordHash, string? fullName = null, string role = "Admin")
    {
        return new Admin
        {
            Username = username,
            PasswordHash = passwordHash,
            FullName = fullName ?? username,
            Role = role // Role có thể là "Admin" hoặc "Staff"
        };
    }
}
