using ToyStore.Domain.Entities;
using ToyStore.Domain.Interfaces;

namespace ToyStore.Domain.States;

/// <summary>
/// State: Đơn hàng đã được xác nhận
/// - Có thể: Ship, Cancel
/// - Không thể: Confirm (đã xác nhận rồi)
/// </summary>
public class ConfirmedState : IOrderState
{
    public string StateName => "Confirmed";

    public IOrderState Confirm(Order order)
    {
        throw new InvalidOperationException("Đơn hàng đã được xác nhận rồi.");
    }

    public IOrderState Ship(Order order)
    {
        // Chuyển sang Shipped state
        order.Status = "Shipped";
        return new ShippedState();
    }

    public IOrderState Cancel(Order order)
    {
        // Cho phép hủy đơn hàng ở trạng thái Confirmed
        order.Status = "Cancelled";
        return new CancelledState();
    }
}
