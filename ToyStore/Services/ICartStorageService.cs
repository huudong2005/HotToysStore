using ToyStore.Models;

namespace ToyStore.Services;

/// <summary>
/// Giỏ hàng session; đồng bộ DB cho khách đã đăng nhập.
/// </summary>
public interface ICartStorageService
{
    Task<ShoppingCart> GetCartAsync(HttpContext context);
    Task SaveCartAsync(HttpContext context, ShoppingCart cart);
    Task RestoreCartAfterLoginAsync(HttpContext context, int customerId);
    Task PersistCartBeforeLogoutAsync(HttpContext context, int customerId);
    Task ClearCartAfterOrderAsync(HttpContext context, int customerId);
    Task RemoveItemsAsync(HttpContext context, IEnumerable<int> cartItemIds, int customerId);
}
