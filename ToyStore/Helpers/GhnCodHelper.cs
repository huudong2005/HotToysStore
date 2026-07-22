namespace ToyStore.Helpers;

/// <summary>
/// Quy tắc thu hộ COD khi đồng bộ vận đơn GHN (giới hạn sandbox/production ~ 50 triệu).
/// </summary>
public static class GhnCodHelper
{
    public const decimal DefaultMaxCodAmount = 50_000_000m;

    public static bool IsCodPayment(string? paymentMethod)
    {
        return string.IsNullOrWhiteSpace(paymentMethod)
            || string.Equals(paymentMethod, "COD", StringComparison.OrdinalIgnoreCase);
    }

    public static bool ExceedsCodLimit(decimal orderTotal, decimal maxCodAmount)
    {
        return orderTotal > maxCodAmount;
    }

    public static string GetLargeOrderConfirmMessage()
    {
        return "Đơn hàng có giá trị lớn, vẫn tiếp tục đặt?";
    }

    public static string GetLargeOrderNoticeMessage(decimal maxCodAmount)
    {
        return $"Đơn hàng trên {maxCodAmount:#,##0}đ. Bạn sẽ được xác nhận trước khi hoàn tất đặt hàng COD.";
    }

    public static string GetLimitExceededMessage(decimal maxCodAmount)
    {
        return $"Đơn hàng trên {maxCodAmount:#,##0}đ không hỗ trợ thu hộ COD qua Giao Hàng Nhanh. "
            + "Vui lòng chọn thanh toán VNPAY để hoàn tất đơn hàng.";
    }
}
