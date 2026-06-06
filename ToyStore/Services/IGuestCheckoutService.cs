using ToyStore.Models;

namespace ToyStore.Services;

public interface IGuestCheckoutService
{
    /// <summary>
    /// Lấy CustomerId từ session đăng nhập, hoặc tạo/cập nhật khách vãng lai theo email.
    /// </summary>
    Task<int> ResolveCustomerIdAsync(HttpContext context, GuestCheckoutInfo guestInfo);

    bool IsGuestPassword(string? passwordHash);
}
