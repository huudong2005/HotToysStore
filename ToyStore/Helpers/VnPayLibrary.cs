using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace ToyStore.Helpers;

/// <summary>
/// Thư viện hỗ trợ khởi tạo URL thanh toán và xác thực chữ ký VNPAY.
/// Tham số sắp xếp Alphabet; encode theo chuẩn PHP urlencode (khoảng trắng = '+').
/// Tham khảo: https://sandbox.vnpayment.vn/apis/docs/thanh-toan-pay/pay.html
/// </summary>
public class VnPayLibrary
{
    private readonly SortedDictionary<string, string> _requestData =
        new(StringComparer.Ordinal);

    private readonly SortedDictionary<string, string> _responseData =
        new(StringComparer.Ordinal);

    public void AddRequestData(string key, string value)
    {
        if (IsExcludedHashParam(key) || string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value))
        {
            return;
        }

        _requestData[key] = value;
    }

    public void AddResponseData(string key, string value)
    {
        if (IsExcludedHashParam(key) || string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value))
        {
            return;
        }

        _responseData[key] = value;
    }

    public string CreateRequestUrl(string baseUrl, string hashSecret)
    {
        var signData = BuildSignData(_requestData);
        var secureHash = ComputeHmacSha512(hashSecret, signData);

        return $"{baseUrl}?{signData}&vnp_SecureHash={secureHash}";
    }

    /// <summary>Chuỗi tham số dùng để ký (debug / kiểm tra cấu hình).</summary>
    public string BuildSignDataPreview() => BuildSignData(_requestData);

    public bool ValidateSignature(string inputHash, string secretKey)
    {
        if (string.IsNullOrWhiteSpace(inputHash))
        {
            return false;
        }

        var signData = BuildSignData(_responseData);
        var computedHash = ComputeHmacSha512(secretKey, signData);

        return string.Equals(computedHash, inputHash, StringComparison.InvariantCultureIgnoreCase);
    }

    public static void LoadResponseFromQuery(VnPayLibrary library, IQueryCollection query)
    {
        foreach (var (key, value) in query)
        {
            if (string.IsNullOrEmpty(key) || !key.StartsWith("vnp_", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (IsExcludedHashParam(key))
            {
                continue;
            }

            library.AddResponseData(key, value.ToString());
        }
    }

    /// <summary>
    /// key=urlencode(value)&amp;... — key giữ nguyên, value encode kiểu PHP urlencode (+ cho space).
    /// Khớp mẫu Java/C# chính thức của VNPAY và URL sandbox (vnp_OrderInfo=Thanh+toan+...).
    /// </summary>
    private static string BuildSignData(SortedDictionary<string, string> data)
    {
        var builder = new StringBuilder();

        foreach (var item in data)
        {
            if (IsExcludedHashParam(item.Key) || string.IsNullOrEmpty(item.Value))
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.Append('&');
            }

            builder.Append(item.Key);
            builder.Append('=');
            builder.Append(VnPayUrlEncode(item.Value));
        }

        return builder.ToString();
    }

    /// <summary>
    /// Tương đương PHP urlencode: space → '+', không dùng %20.
    /// VNPAY sandbox mẫu: vnp_OrderInfo=Thanh+toan+don+hang+%3A5
    /// </summary>
    public static string VnPayUrlEncode(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return WebUtility.UrlEncode(value).Replace("%20", "+");
    }

    private static bool IsExcludedHashParam(string key)
    {
        return key.Equals("vnp_SecureHash", StringComparison.OrdinalIgnoreCase)
            || key.Equals("vnp_SecureHashType", StringComparison.OrdinalIgnoreCase);
    }

    public static string ComputeHmacSha512(string key, string inputData)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var inputBytes = Encoding.UTF8.GetBytes(inputData);

        using var hmac = new HMACSHA512(keyBytes);
        var hashBytes = hmac.ComputeHash(inputBytes);

        var hash = new StringBuilder();
        foreach (var b in hashBytes)
        {
            hash.Append(b.ToString("x2"));
        }

        return hash.ToString();
    }
}
