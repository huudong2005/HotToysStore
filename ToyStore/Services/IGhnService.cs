using System.Text.Json.Serialization;
using ToyStore.Models;

namespace ToyStore.Services;

public interface IGhnService
{
    Task<IReadOnlyList<GhnProvinceDto>> GetProvincesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GhnDistrictDto>> GetDistrictsAsync(int provinceId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GhnWardDto>> GetWardsAsync(int districtId, CancellationToken cancellationToken = default);

    Task<GhnFeeResult?> CalculateFeeAsync(
        int toDistrictId,
        string toWardCode,
        int weight = 1000,
        CancellationToken cancellationToken = default);

    Task<string?> CreateShippingOrderAsync(
        GhnCreateOrderRequest request,
        CancellationToken cancellationToken = default);
}

public class GhnProvinceDto
{
    [JsonPropertyName("ProvinceID")]
    public int ProvinceId { get; set; }

    [JsonPropertyName("ProvinceName")]
    public string ProvinceName { get; set; } = string.Empty;
}

public class GhnDistrictDto
{
    [JsonPropertyName("DistrictID")]
    public int DistrictId { get; set; }

    [JsonPropertyName("ProvinceID")]
    public int ProvinceId { get; set; }

    [JsonPropertyName("DistrictName")]
    public string DistrictName { get; set; } = string.Empty;
}

public class GhnWardDto
{
    [JsonPropertyName("WardCode")]
    public string WardCode { get; set; } = string.Empty;

    [JsonPropertyName("DistrictID")]
    public int DistrictId { get; set; }

    [JsonPropertyName("WardName")]
    public string WardName { get; set; } = string.Empty;
}

public class GhnFeeResult
{
    public decimal Total { get; set; }

    public decimal ServiceFee { get; set; }
}

public class GhnCreateOrderRequest
{
    public int OrderId { get; set; }

    public string ToName { get; set; } = string.Empty;

    public string ToPhone { get; set; } = string.Empty;

    public string ToAddress { get; set; } = string.Empty;

    public int ToDistrictId { get; set; }

    public string ToWardCode { get; set; } = string.Empty;

    public decimal CodAmount { get; set; }

    public string? PaymentMethod { get; set; }

    public int Weight { get; set; } = 1000;

    public string? Note { get; set; }
}
