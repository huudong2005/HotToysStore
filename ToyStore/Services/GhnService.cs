using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using ToyStore.Helpers;
using ToyStore.Models;

namespace ToyStore.Services;

public class GhnService : IGhnService
{
    private readonly HttpClient _httpClient;
    private readonly GhnSettings _settings;
    private readonly ILogger<GhnService> _logger;

    public bool LastRequestUsedFallback { get; private set; }

    public string? LastErrorMessage { get; private set; }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public GhnService(HttpClient httpClient, IOptions<GhnSettings> settings, ILogger<GhnService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<GhnProvinceDto>> GetProvincesAsync(CancellationToken cancellationToken = default)
    {
        LastRequestUsedFallback = false;
        LastErrorMessage = null;

        var apiData = await GetMasterDataAsync<GhnProvinceDto>(
            "/shiip/public-api/master-data/province",
            null,
            cancellationToken);

        if (apiData.Count > 0)
        {
            return apiData;
        }

        return UseFallbackOrEmpty(
            GhnMasterDataFallback.GetProvinces(),
            "Không tải được Tỉnh/Thành từ GHN (Token có thể hết hạn). Đang dùng dữ liệu mẫu.");
    }

    public async Task<IReadOnlyList<GhnDistrictDto>> GetDistrictsAsync(int provinceId, CancellationToken cancellationToken = default)
    {
        LastRequestUsedFallback = false;

        var apiData = await GetMasterDataAsync<GhnDistrictDto>(
            $"/shiip/public-api/master-data/district?province_id={provinceId}",
            new { province_id = provinceId },
            cancellationToken,
            usePost: true);

        if (apiData.Count > 0)
        {
            return apiData;
        }

        return UseFallbackOrEmpty(GhnMasterDataFallback.GetDistricts(provinceId), null);
    }

    public async Task<IReadOnlyList<GhnWardDto>> GetWardsAsync(int districtId, CancellationToken cancellationToken = default)
    {
        LastRequestUsedFallback = false;

        var apiData = await GetMasterDataAsync<GhnWardDto>(
            $"/shiip/public-api/master-data/ward?district_id={districtId}",
            new { district_id = districtId },
            cancellationToken,
            usePost: true);

        if (apiData.Count > 0)
        {
            return apiData;
        }

        return UseFallbackOrEmpty(GhnMasterDataFallback.GetWards(districtId), null);
    }

    public async Task<GhnFeeResult?> CalculateFeeAsync(
        int toDistrictId,
        string toWardCode,
        int weight = 1000,
        CancellationToken cancellationToken = default)
    {
        if (toDistrictId <= 0 || string.IsNullOrWhiteSpace(toWardCode))
        {
            return null;
        }

        var payload = new
        {
            service_type_id = _settings.ServiceTypeId,
            from_district_id = _settings.FromDistrictId,
            from_ward_code = _settings.FromWardCode,
            to_district_id = toDistrictId,
            to_ward_code = toWardCode,
            weight = weight > 0 ? weight : _settings.DefaultWeightGram,
            length = 20,
            width = 20,
            height = 10,
            insurance_value = 0,
            coupon = (string?)null
        };

        try
        {
            var response = await SendAsync<GhnFeeResponse>(
                HttpMethod.Post,
                "/shiip/public-api/v2/shipping-order/fee",
                payload,
                includeShopId: true,
                cancellationToken);

            if (response?.Data == null)
            {
                return null;
            }

            return new GhnFeeResult
            {
                Total = response.Data.Total,
                ServiceFee = response.Data.ServiceFee
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GHN CalculateFee failed for district {DistrictId}", toDistrictId);
            return null;
        }
    }

    public async Task<string?> CreateShippingOrderAsync(
        GhnCreateOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null || request.ToDistrictId <= 0 || string.IsNullOrWhiteSpace(request.ToWardCode))
        {
            throw new InvalidOperationException("Thiếu thông tin Quận/Phường GHN để tạo vận đơn.");
        }

        var toName = SanitizeName(request.ToName);
        var toPhone = SanitizePhone(request.ToPhone, _settings.FromPhone);
        var toAddress = SanitizeAddress(request.ToAddress);

        var weight = request.Weight > 0 ? request.Weight : _settings.DefaultWeightGram;
        var requestedCod = string.Equals(request.PaymentMethod, "VNPAY", StringComparison.OrdinalIgnoreCase)
            ? 0m
            : Math.Max(0, request.CodAmount);

        int codAmount;
        var note = request.Note ?? string.Empty;

        if (requestedCod > _settings.MaxCodAmount)
        {
            // GHN không thu hộ quá MaxCodAmount — vẫn tạo vận đơn, thu COD trực tiếp tại cửa.
            codAmount = 0;
            note = $"{note} [COD {requestedCod:#,##0}đ — thu tại cửa]".Trim();
        }
        else
        {
            codAmount = (int)Math.Round(requestedCod, MidpointRounding.AwayFromZero);
        }

        var payload = new
        {
            payment_type_id = 2,
            service_type_id = _settings.ServiceTypeId,
            required_note = "KHONGCHOXEMHANG",
            to_name = toName,
            to_phone = toPhone,
            to_address = toAddress,
            to_ward_code = request.ToWardCode.Trim(),
            to_district_id = request.ToDistrictId,
            from_name = _settings.FromName,
            from_phone = _settings.FromPhone,
            from_address = _settings.FromAddress,
            from_district_id = _settings.FromDistrictId,
            from_ward_code = _settings.FromWardCode,
            return_phone = _settings.FromPhone,
            return_address = _settings.FromAddress,
            return_district_id = _settings.FromDistrictId,
            return_ward_code = _settings.FromWardCode,
            client_order_code = $"TOY-{request.OrderId}",
            content = $"Đơn hàng ToyStore #{request.OrderId}",
            note,
            weight,
            length = 20,
            width = 20,
            height = 10,
            cod_amount = codAmount,
            items = new[]
            {
                new
                {
                    name = "Sản phẩm ToyStore",
                    quantity = 1,
                    weight,
                    length = 20,
                    width = 20,
                    height = 10
                }
            }
        };

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "/shiip/public-api/v2/shipping-order/create");

        var token = _settings.Token?.Trim();
        if (!string.IsNullOrWhiteSpace(token))
        {
            httpRequest.Headers.TryAddWithoutValidation("Token", token);
        }

        if (_settings.ShopId > 0)
        {
            httpRequest.Headers.TryAddWithoutValidation("ShopId", _settings.ShopId.ToString());
        }

        var json = JsonSerializer.Serialize(payload, JsonOptions);
        httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            LastErrorMessage = responseContent;
            _logger.LogWarning(
                "GHN CreateShippingOrder HTTP {Status} for order {OrderId}: {Body}",
                (int)response.StatusCode,
                request.OrderId,
                responseContent);
            throw new Exception($"Lỗi GHN: {responseContent}");
        }

        GhnCreateOrderResponse? apiResponse;
        try
        {
            apiResponse = JsonSerializer.Deserialize<GhnCreateOrderResponse>(responseContent, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new Exception($"Lỗi GHN: Không đọc được phản hồi API. {responseContent}", ex);
        }

        if (apiResponse == null || apiResponse.Code != 200)
        {
            LastErrorMessage = apiResponse?.Message ?? responseContent;
            _logger.LogWarning(
                "GHN CreateShippingOrder rejected order {OrderId}: {Body}",
                request.OrderId,
                responseContent);
            throw new Exception($"Lỗi GHN: {responseContent}");
        }

        if (string.IsNullOrWhiteSpace(apiResponse.Data?.OrderCode))
        {
            throw new Exception($"Lỗi GHN: Không nhận được mã vận đơn. {responseContent}");
        }

        return apiResponse.Data.OrderCode;
    }

    private static string SanitizeName(string? name)
    {
        return string.IsNullOrWhiteSpace(name) ? "Khách hàng" : name.Trim();
    }

    private static string SanitizePhone(string? phone, string fallbackPhone)
    {
        var fallback = string.IsNullOrWhiteSpace(fallbackPhone) ? "0378113807" : fallbackPhone.Trim();
        if (string.IsNullOrWhiteSpace(phone))
        {
            return fallback;
        }

        var digits = new string(phone.Where(char.IsDigit).ToArray());
        if (digits.Length is >= 9 and <= 11)
        {
            return digits;
        }

        return fallback;
    }

    private static string SanitizeAddress(string? address)
    {
        return string.IsNullOrWhiteSpace(address) ? "Địa chỉ nhận hàng" : address.Trim();
    }

    private IReadOnlyList<T> UseFallbackOrEmpty<T>(IReadOnlyList<T> fallbackData, string? logMessage)
    {
        if (_settings.EnableLocalFallback && fallbackData.Count > 0)
        {
            LastRequestUsedFallback = true;
            if (!string.IsNullOrWhiteSpace(logMessage))
            {
                LastErrorMessage = logMessage;
                _logger.LogWarning("{Message}", logMessage);
            }

            return fallbackData;
        }

        return Array.Empty<T>();
    }

    private async Task<IReadOnlyList<T>> GetMasterDataAsync<T>(
        string path,
        object? postBody,
        CancellationToken cancellationToken,
        bool usePost = false)
    {
        try
        {
            var response = usePost && postBody != null
                ? await SendAsync<GhnListResponse<T>>(HttpMethod.Post, path, postBody, includeShopId: false, cancellationToken)
                : await SendAsync<GhnListResponse<T>>(HttpMethod.Get, path, null, includeShopId: false, cancellationToken);

            if (response?.Code == 200 && response.Data != null && response.Data.Count > 0)
            {
                return response.Data;
            }

            if (response?.Code == 401)
            {
                LastErrorMessage = "Token GHN không hợp lệ hoặc đã hết hạn. Cập nhật Ghn:Token trong appsettings.json.";
            }

            return Array.Empty<T>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GHN master data failed: {Path}", path);
            return Array.Empty<T>();
        }
    }

    private async Task<TResponse?> SendAsync<TResponse>(
        HttpMethod method,
        string path,
        object? body,
        bool includeShopId,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, path);

        var token = _settings.Token?.Trim();
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.TryAddWithoutValidation("Token", token);
        }

        if (includeShopId && _settings.ShopId > 0)
        {
            request.Headers.TryAddWithoutValidation("ShopId", _settings.ShopId.ToString());
        }

        if (body != null)
        {
            var json = JsonSerializer.Serialize(body, JsonOptions);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }
        else
        {
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        GhnApiResponse? apiBody = null;
        try
        {
            apiBody = JsonSerializer.Deserialize<GhnApiResponse>(content, JsonOptions);
        }
        catch
        {
            // ignore
        }

        if (!response.IsSuccessStatusCode || (apiBody != null && apiBody.Code != 200))
        {
            LastErrorMessage = apiBody?.Message ?? content;
            _logger.LogWarning("GHN HTTP {Status}: {Body}", (int)response.StatusCode, content);
            return default;
        }

        return JsonSerializer.Deserialize<TResponse>(content, JsonOptions);
    }

    private class GhnApiResponse
    {
        public int Code { get; set; }

        public string? Message { get; set; }
    }

    private class GhnListResponse<T> : GhnApiResponse
    {
        public List<T>? Data { get; set; }
    }

    private class GhnFeeResponse : GhnApiResponse
    {
        public GhnFeeData? Data { get; set; }
    }

    private class GhnFeeData
    {
        public decimal Total { get; set; }

        [JsonPropertyName("service_fee")]
        public decimal ServiceFee { get; set; }
    }

    private class GhnCreateOrderResponse : GhnApiResponse
    {
        public GhnCreateOrderData? Data { get; set; }
    }

    private class GhnCreateOrderData
    {
        [JsonPropertyName("order_code")]
        public string? OrderCode { get; set; }
    }
}
