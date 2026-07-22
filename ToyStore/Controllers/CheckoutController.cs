using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ToyStore.Models;
using ToyStore.Services;

namespace ToyStore.Controllers;

/// <summary>
/// API hỗ trợ trang Checkout — địa chỉ GHN và tính phí vận chuyển (AJAX).
/// </summary>
public class CheckoutController : Controller
{
    private readonly IGhnService _ghnService;
    private readonly GhnSettings _ghnSettings;

    public CheckoutController(IGhnService ghnService, IOptions<GhnSettings> ghnSettings)
    {
        _ghnService = ghnService;
        _ghnSettings = ghnSettings.Value;
    }

    [HttpGet]
    public async Task<IActionResult> Provinces(CancellationToken cancellationToken)
    {
        var provinces = await _ghnService.GetProvincesAsync(cancellationToken);
        var ghn = _ghnService as GhnService;

        return Json(new
        {
            success = provinces.Count > 0,
            data = provinces.Select(p => new { provinceId = p.ProvinceId, provinceName = p.ProvinceName }),
            isFallback = ghn?.LastRequestUsedFallback == true,
            message = ghn?.LastErrorMessage
        });
    }

    [HttpGet]
    public async Task<IActionResult> Districts(int provinceId, CancellationToken cancellationToken)
    {
        if (provinceId <= 0)
        {
            return Json(new { success = false, message = "provinceId không hợp lệ." });
        }

        var districts = await _ghnService.GetDistrictsAsync(provinceId, cancellationToken);
        var ghn = _ghnService as GhnService;

        return Json(new
        {
            success = districts.Count > 0,
            data = districts.Select(d => new { districtId = d.DistrictId, districtName = d.DistrictName, provinceId = d.ProvinceId }),
            isFallback = ghn?.LastRequestUsedFallback == true
        });
    }

    [HttpGet]
    public async Task<IActionResult> Wards(int districtId, CancellationToken cancellationToken)
    {
        if (districtId <= 0)
        {
            return Json(new { success = false, message = "districtId không hợp lệ." });
        }

        var wards = await _ghnService.GetWardsAsync(districtId, cancellationToken);
        var ghn = _ghnService as GhnService;

        return Json(new
        {
            success = wards.Count > 0,
            data = wards.Select(w => new { wardCode = w.WardCode, wardName = w.WardName, districtId = w.DistrictId }),
            isFallback = ghn?.LastRequestUsedFallback == true
        });
    }

    [HttpPost]
    public async Task<IActionResult> CalculateFee(
        int toDistrictId,
        string toWardCode,
        int weight = 1000,
        CancellationToken cancellationToken = default)
    {
        if (toDistrictId <= 0 || string.IsNullOrWhiteSpace(toWardCode))
        {
            return Json(new
            {
                success = false,
                message = "Vui lòng chọn đủ Tỉnh/Quận/Phường.",
                fee = _ghnSettings.FallbackShippingFee
            });
        }

        var feeResult = await _ghnService.CalculateFeeAsync(toDistrictId, toWardCode, weight, cancellationToken);
        if (feeResult == null)
        {
            return Json(new
            {
                success = false,
                message = "Không tính được phí GHN. Áp dụng phí mặc định.",
                fee = _ghnSettings.FallbackShippingFee,
                isFallback = true
            });
        }

        return Json(new
        {
            success = true,
            fee = feeResult.Total,
            serviceFee = feeResult.ServiceFee
        });
    }
}
