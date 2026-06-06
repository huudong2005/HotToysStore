using ToyStore.Domain.Interfaces;
using ToyStore.Domain.States;

namespace ToyStore.Domain.Entities;

/// <summary>
/// Extension methods cho Order entity để sử dụng State Pattern
/// </summary>
public static class OrderExtensions
{
    /// <summary>
    /// Lấy current state của Order
    /// </summary>
    public static IOrderState GetState(this Order order)
    {
        return OrderStateFactory.GetState(order);
    }

    /// <summary>
    /// Xác nhận đơn hàng (chuyển từ Pending sang Confirmed)
    /// </summary>
    /// <exception cref="InvalidOperationException">Nếu không thể xác nhận ở state hiện tại</exception>
    public static void Confirm(this Order order)
    {
        var currentState = order.GetState();
        currentState.Confirm(order);
    }

    /// <summary>
    /// Giao hàng (chuyển từ Confirmed sang Shipped)
    /// </summary>
    /// <exception cref="InvalidOperationException">Nếu không thể giao hàng ở state hiện tại</exception>
    public static void Ship(this Order order)
    {
        var currentState = order.GetState();
        currentState.Ship(order);
    }

    /// <summary>
    /// Hủy đơn hàng
    /// </summary>
    /// <exception cref="InvalidOperationException">Nếu không thể hủy ở state hiện tại</exception>
    public static void Cancel(this Order order)
    {
        var currentState = order.GetState();
        currentState.Cancel(order);
    }

    /// <summary>
    /// Kiểm tra xem Order có thể hủy được không
    /// </summary>
    public static bool CanCancel(this Order order)
    {
        var state = order.GetState();
        // Chỉ Pending và Confirmed có thể hủy
        return state.StateName == "Pending" || state.StateName == "Confirmed";
    }

    /// <summary>
    /// Kiểm tra xem Order có thể xác nhận được không
    /// </summary>
    public static bool CanConfirm(this Order order)
    {
        var state = order.GetState();
        // Chỉ Pending có thể xác nhận
        return state.StateName == "Pending";
    }

    /// <summary>
    /// Kiểm tra xem Order có thể giao hàng được không
    /// </summary>
    public static bool CanShip(this Order order)
    {
        var state = order.GetState();
        // Chỉ Confirmed có thể giao hàng
        return state.StateName == "Confirmed";
    }
}
