using ToyStore.Domain.Entities;
using ToyStore.Domain.Interfaces;

namespace ToyStore.Domain.States;

/// <summary>
/// State: Đơn hàng đang chờ xử lý
/// - Có thể: Confirm, Cancel
/// - Không thể: Ship
/// </summary>
public class PendingState : IOrderState
{
    public string StateName => "Pending";

    public IOrderState Confirm(Order order)
    {
        // Chuyển sang Confirmed state
        order.Status = "Confirmed";
        return new ConfirmedState();
    }

    public IOrderState Ship(Order order)
    {
        throw new InvalidOperationException("Không thể giao hàng khi đơn hàng đang ở trạng thái Pending. Vui lòng xác nhận đơn hàng trước.");
    }

    public IOrderState Cancel(Order order)
    {
        // Cho phép hủy đơn hàng ở trạng thái Pending
        order.Status = "Cancelled";
        return new CancelledState();
    }
}
