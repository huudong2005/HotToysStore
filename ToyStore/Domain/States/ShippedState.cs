using ToyStore.Domain.Entities;
using ToyStore.Domain.Interfaces;

namespace ToyStore.Domain.States;

/// <summary>
/// State: Đơn hàng đã được giao
/// - Không thể: Confirm, Ship, Cancel (đã giao rồi)
/// </summary>
public class ShippedState : IOrderState
{
    public string StateName => "Shipped";

    public IOrderState Confirm(Order order)
    {
        throw new InvalidOperationException("Đơn hàng đã được giao, không thể xác nhận lại.");
    }

    public IOrderState Ship(Order order)
    {
        throw new InvalidOperationException("Đơn hàng đã được giao rồi.");
    }

    public IOrderState Cancel(Order order)
    {
        throw new InvalidOperationException("Không thể hủy đơn hàng khi đã được giao. Đơn hàng đã ở trạng thái Shipped.");
    }
}
