using ToyStore.Models;

namespace ToyStore.Services;

/// <summary>
/// Lưu giỏ hàng của khách đã đăng ký vào bảng Cart / CartItem (Oracle).
/// </summary>
public interface ICustomerCartPersistenceService
{
    Task SaveAsync(int customerId, ShoppingCart cart);
    Task<ShoppingCart> LoadAsync(int customerId);
    Task ClearAsync(int customerId);
}
