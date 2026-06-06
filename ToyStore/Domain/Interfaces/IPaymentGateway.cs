using ToyStore.Domain.Entities;

namespace ToyStore.Domain.Interfaces;

/// <summary>
/// Adapter Pattern: Abstraction cho cổng thanh toán.
/// Các gateway cụ thể (VnPay, Momo, v.v.) sẽ implement interface này.
/// </summary>
public interface IPaymentGateway
{
    Task<PaymentResult> ProcessPaymentAsync(Order order, decimal amount, string? paymentMethod);
}

