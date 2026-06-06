namespace ToyStore.Models;

public class VnPaySettings
{
    public string Url { get; set; } = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
    public string TmnCode { get; set; } = string.Empty;
    public string HashSecret { get; set; } = string.Empty;
    public string Version { get; set; } = "2.1.0";
    public string Command { get; set; } = "pay";
    public string ReturnUrl { get; set; } = string.Empty;
    public string IpnUrl { get; set; } = string.Empty;
    /// <summary>
    /// Tự lấy ReturnUrl từ request hiện tại (khuyến nghị khi dev localhost).
    /// </summary>
    public bool UseDynamicReturnUrl { get; set; } = true;
}
