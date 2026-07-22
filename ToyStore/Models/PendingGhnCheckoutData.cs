namespace ToyStore.Models;

/// <summary>
/// Thông tin GHN tạm lưu session khi khách chọn VNPAY trước khi redirect sang cổng thanh toán.
/// </summary>
public class PendingGhnCheckoutData
{
    public int? GhnProvinceId { get; set; }

    public int? GhnDistrictId { get; set; }

    public string? GhnWardCode { get; set; }

    public string? ShippingAddress { get; set; }

    public string? DeliveryStreet { get; set; }

    public decimal ShippingFee { get; set; }
}
