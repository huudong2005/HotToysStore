using ToyStore.Domain.Entities;
using ToyStore.Domain.Interfaces;

namespace ToyStore.Domain.States;

/// <summary>
/// State: Đơn hàng đã bị hủy
/// - Không thể: Confirm, Ship, Cancel (đã hủy rồi)
/// </summary>
public class CancelledState : IOrderState
{
    public string StateName => "Cancelled";

    public IOrderState Confirm(Order order)
    {
        throw new InvalidOperationException("Không thể xác nhận đơn hàng đã bị hủy.");
    }

    public IOrderState Ship(Order order)
    {
        throw new InvalidOperationException("Không thể giao đơn hàng đã bị hủy.");
    }

    public IOrderState Cancel(Order order)
    {
        throw new InvalidOperationException("Đơn hàng đã bị hủy rồi.");
    }
}
