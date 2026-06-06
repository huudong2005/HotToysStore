namespace ToyStore.Models;

public class VnPayPaymentResultViewModel
{
    public bool IsSuccess { get; set; }
    public string OrderId { get; set; } = string.Empty;
    public string TransactionNo { get; set; } = string.Empty;
    public string ResponseCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Amount { get; set; } = string.Empty;
    /// <summary>Số tiền hiển thị (VNPAY gửi amount × 100).</summary>
    public string DisplayAmount { get; set; } = string.Empty;
    public string OrderInfo { get; set; } = string.Empty;
    public string PayDate { get; set; } = string.Empty;
    public string BankCode { get; set; } = string.Empty;
    public string BankTranNo { get; set; } = string.Empty;
    public string CardType { get; set; } = string.Empty;
    public bool IsSignatureValid { get; set; }
}
