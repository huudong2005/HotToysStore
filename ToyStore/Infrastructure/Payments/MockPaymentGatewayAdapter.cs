using ToyStore.Domain.Entities;
using ToyStore.Domain.Interfaces;

namespace ToyStore.Infrastructure.Payments;

/// <summary>
/// Adapter Pattern: Adapter mock cho cổng thanh toán.
/// Hiện tại mô phỏng luôn thành công để minh họa kiến trúc.
/// </summary>
public class MockPaymentGatewayAdapter : IPaymentGateway
{
    public Task<PaymentResult> ProcessPaymentAsync(Order order, decimal amount, string? paymentMethod)
    {
        // Ở môi trường thực tế, tại đây sẽ gọi SDK/cổng thanh toán bên ngoài.
        // Để đơn giản cho đồ án, mock luôn thành công.
        var result = new PaymentResult
        {
            Success = true,
            TransactionId = $"MOCK-{order.OrderId}-{DateTime.UtcNow.Ticks}",
            Message = "Mock payment processed successfully"
        };

        return Task.FromResult(result);
    }
}

