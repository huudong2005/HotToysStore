using ToyStore.Domain.Entities;
using ToyStore.Domain.Interfaces;

namespace ToyStore.Domain.States;

/// <summary>
/// Factory class để tạo OrderState từ Status string
/// </summary>
public static class OrderStateFactory
{
    /// <summary>
    /// Tạo IOrderState từ Status string của Order
    /// </summary>
    /// <param name="status">Status string (Pending, Confirmed, Shipped, Cancelled)</param>
    /// <returns>IOrderState tương ứng</returns>
    public static IOrderState CreateState(string? status)
    {
        return (status?.Trim() ?? "Pending") switch
        {
            "Pending" => new PendingState(),
            "Confirmed" => new ConfirmedState(),
            "Shipped" => new ShippedState(),
            "Cancelled" => new CancelledState(),
            _ => new PendingState() // Default to Pending if unknown
        };
    }

    /// <summary>
    /// Lấy IOrderState từ Order entity
    /// </summary>
    /// <param name="order">Order entity</param>
    /// <returns>IOrderState tương ứng với Status của Order</returns>
    public static IOrderState GetState(Order order)
    {
        return CreateState(order.Status);
    }
}
