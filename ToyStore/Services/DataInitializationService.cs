using Microsoft.EntityFrameworkCore;
using ToyStore.Domain.Interfaces;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Services;

/// <summary>
/// Service để khởi tạo dữ liệu mặc định (admin account)
/// </summary>
public class DataInitializationService
{
    private readonly ToyStoreContext _context;
    private readonly IUserFactory _userFactory;
    private readonly IAuthService _authService;

    public DataInitializationService(
        ToyStoreContext context, 
        IUserFactory userFactory,
        IAuthService authService)
    {
        _context = context;
        _userFactory = userFactory;
        _authService = authService;
    }

    /// <summary>
    /// Khởi tạo admin mặc định nếu chưa tồn tại
    /// </summary>
    public async Task InitializeDefaultAdminAsync()
    {
        // Kiểm tra xem đã có admin với username "admin" chưa
        var existingAdmin = await _context.Admins
            .FirstOrDefaultAsync(a => a.Username == "admin");

        if (existingAdmin == null)
        {
            // Sử dụng UserFactory để tạo admin mặc định
            var passwordHash = _authService.HashPassword("admin123");
            var defaultAdmin = _userFactory.CreateAdmin(
                username: "admin",
                passwordHash: passwordHash,
                fullName: "Administrator",
                role: "Admin"
            );

            _context.Admins.Add(defaultAdmin);
            await _context.SaveChangesAsync();
        }
    }
}
