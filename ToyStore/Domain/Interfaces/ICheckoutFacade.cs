using ToyStore.Domain.Entities;
using ToyStore.Models;

namespace ToyStore.Domain.Interfaces;

/// <summary>
/// Interface định nghĩa Facade cho quy trình thanh toán (Checkout)
/// </summary>
public interface ICheckoutFacade
{
    /// <summary>
    /// Thực hiện quy trình đặt hàng hoàn chỉnh:
    /// 1. Kiểm tra tồn kho
    /// 2. Tính giá (Strategy Pattern)
    /// 3. Tạo đơn hàng (Unit of Work)
    /// 4. Cập nhật Stock
    /// 5. Xóa Session giỏ hàng
    /// </summary>
    /// <param name="cart">Giỏ hàng cần thanh toán</param>
    /// <param name="customerId">ID khách hàng</param>
    /// <param name="paymentMethod">Phương thức thanh toán</param>
    /// <returns>Order đã được tạo thành công</returns>
    /// <exception cref="InvalidOperationException">Nếu giỏ hàng trống hoặc không đủ tồn kho</exception>
    Task<Order> PlaceOrderAsync(ShoppingCart cart, int customerId, string? paymentMethod = null);
}
