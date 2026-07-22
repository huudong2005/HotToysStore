using ToyStore.Helpers;

namespace ToyStore.Models;

public class GhnSettings
{
    public string BaseUrl { get; set; } = "https://dev-online-gateway.ghn.vn";

    public string Token { get; set; } = string.Empty;

    public int ShopId { get; set; }

    public int FromDistrictId { get; set; } = 1442;

    public string FromWardCode { get; set; } = "21211";

    public string FromName { get; set; } = "ToyStore";

    public string FromPhone { get; set; } = "0900000000";

    public string FromAddress { get; set; } = "Kho ToyStore";

    public int ServiceTypeId { get; set; } = 2;

    public int DefaultWeightGram { get; set; } = 1000;

    public decimal FallbackShippingFee { get; set; } = 30_000m;

    /// <summary>
    /// Giới hạn tiền thu hộ COD khi tạo vận đơn GHN (mặc định 50.000.000đ).
    /// </summary>
    public decimal MaxCodAmount { get; set; } = GhnCodHelper.DefaultMaxCodAmount;

    /// <summary>
    /// Khi true: nếu GHN trả lỗi (Token hết hạn, 401...) sẽ dùng danh sách Tỉnh/Quận/Phường mẫu để dev UI.
    /// </summary>
    public bool EnableLocalFallback { get; set; } = true;
}
