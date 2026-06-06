using ToyStore.Domain.Entities;

namespace ToyStore.Domain.Interfaces;

/// <summary>
/// Interface định nghĩa các hành vi của Order State
/// </summary>
public interface IOrderState
{
    /// <summary>
    /// Tên của state (Pending, Confirmed, Shipped, Cancelled)
    /// </summary>
    string StateName { get; }

    /// <summary>
    /// Xác nhận đơn hàng (chuyển từ Pending sang Confirmed)
    /// </summary>
    /// <param name="order">Đơn hàng cần xác nhận</param>
    /// <returns>State mới sau khi xác nhận</returns>
    /// <exception cref="InvalidOperationException">Nếu không thể xác nhận ở state hiện tại</exception>
    IOrderState Confirm(Order order);

    /// <summary>
    /// Giao hàng (chuyển từ Confirmed sang Shipped)
    /// </summary>
    /// <param name="order">Đơn hàng cần giao</param>
    /// <returns>State mới sau khi giao hàng</returns>
    /// <exception cref="InvalidOperationException">Nếu không thể giao hàng ở state hiện tại</exception>
    IOrderState Ship(Order order);

    /// <summary>
    /// Hủy đơn hàng
    /// </summary>
    /// <param name="order">Đơn hàng cần hủy</param>
    /// <returns>State mới sau khi hủy</returns>
    /// <exception cref="InvalidOperationException">Nếu không thể hủy ở state hiện tại</exception>
    IOrderState Cancel(Order order);
}
