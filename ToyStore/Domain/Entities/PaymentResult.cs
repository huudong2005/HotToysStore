namespace ToyStore.Domain.Entities;

/// <summary>
/// Kết quả thanh toán từ cổng thanh toán (được sử dụng bởi Adapter Pattern).
/// </summary>
public class PaymentResult
{
    public bool Success { get; set; }
    public string? TransactionId { get; set; }
    public string? Message { get; set; }
}

