using ToyStore.Domain.Entities;
using ToyStore.Domain.Interfaces;
using ToyStore.Models;

namespace ToyStore.Services;

public class GhnOrderShippingService
{
    private readonly IGhnService _ghnService;
    private readonly GhnSettings _settings;
    private readonly IUnitOfWork _unitOfWork;

    public GhnOrderShippingService(
        IGhnService ghnService,
        Microsoft.Extensions.Options.IOptions<GhnSettings> settings,
        IUnitOfWork unitOfWork)
    {
        _ghnService = ghnService;
        _settings = settings.Value;
        _unitOfWork = unitOfWork;
    }

    public async Task SaveShippingAddressAsync(
        Order order,
        PendingGhnCheckoutData? ghnData,
        CancellationToken cancellationToken = default)
    {
        if (order == null || ghnData == null)
        {
            return;
        }

        var address = await ResolveGhnShippingAddressAsync(
            ghnData.ShippingAddress,
            ghnData.DeliveryStreet,
            ghnData.GhnProvinceId,
            ghnData.GhnDistrictId,
            ghnData.GhnWardCode,
            cancellationToken);

        if (string.IsNullOrWhiteSpace(address))
        {
            return;
        }

        order.ShippingAddress = address;
        _unitOfWork.Orders.Update(order);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<string?> CreateShippingOrderAsync(
        Order order,
        Customer? customer,
        PendingGhnCheckoutData? ghnData,
        string? guestFullName = null,
        string? guestPhone = null,
        string? guestAddress = null,
        CancellationToken cancellationToken = default)
    {
        if (order == null
            || ghnData?.GhnDistrictId is not > 0
            || string.IsNullOrWhiteSpace(ghnData.GhnWardCode))
        {
            return null;
        }

        var toName = !string.IsNullOrWhiteSpace(customer?.FullName)
            ? customer!.FullName
            : guestFullName ?? "Khách hàng";
        var toPhone = !string.IsNullOrWhiteSpace(customer?.Phone)
            ? customer!.Phone!
            : guestPhone ?? _settings.FromPhone;
        var street = !string.IsNullOrWhiteSpace(ghnData.DeliveryStreet)
            ? ghnData.DeliveryStreet
            : order.ShippingAddress ?? guestAddress ?? customer?.Address ?? string.Empty;

        var orderCode = await _ghnService.CreateShippingOrderAsync(new GhnCreateOrderRequest
        {
            OrderId = order.OrderId,
            ToName = toName,
            ToPhone = toPhone,
            ToAddress = street,
            ToDistrictId = ghnData.GhnDistrictId.Value,
            ToWardCode = ghnData.GhnWardCode,
            PaymentMethod = order.PaymentMethod,
            CodAmount = order.TotalAmount,
            Weight = _settings.DefaultWeightGram,
            Note = $"ToyStore order #{order.OrderId}"
        }, cancellationToken);

        if (string.IsNullOrWhiteSpace(orderCode))
        {
            throw new InvalidOperationException("Lỗi GHN: API không trả về mã vận đơn.");
        }

        order.ShippingCode = orderCode;
        order.DeliveryMethod = "GHN";
        _unitOfWork.Orders.Update(order);
        await _unitOfWork.SaveChangesAsync();

        return orderCode;
    }

    public async Task<string?> ResolveGhnShippingAddressAsync(
        string? shippingAddress,
        string? deliveryStreet,
        int? ghnProvinceId,
        int? ghnDistrictId,
        string? ghnWardCode,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(shippingAddress)
            && shippingAddress.Contains(',', StringComparison.Ordinal))
        {
            return shippingAddress.Trim();
        }

        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(deliveryStreet))
        {
            parts.Add(deliveryStreet.Trim());
        }

        if (ghnDistrictId is > 0 && !string.IsNullOrWhiteSpace(ghnWardCode))
        {
            var wards = await _ghnService.GetWardsAsync(ghnDistrictId.Value, cancellationToken);
            var ward = wards.FirstOrDefault(w =>
                string.Equals(w.WardCode, ghnWardCode, StringComparison.OrdinalIgnoreCase));
            if (ward != null && !string.IsNullOrWhiteSpace(ward.WardName))
            {
                parts.Add(ward.WardName);
            }

            if (ghnProvinceId is > 0)
            {
                var districts = await _ghnService.GetDistrictsAsync(ghnProvinceId.Value, cancellationToken);
                var district = districts.FirstOrDefault(d => d.DistrictId == ghnDistrictId.Value);
                if (district != null && !string.IsNullOrWhiteSpace(district.DistrictName))
                {
                    parts.Add(district.DistrictName);
                }

                var provinces = await _ghnService.GetProvincesAsync(cancellationToken);
                var province = provinces.FirstOrDefault(p => p.ProvinceId == ghnProvinceId.Value);
                if (province != null && !string.IsNullOrWhiteSpace(province.ProvinceName))
                {
                    parts.Add(province.ProvinceName);
                }
            }
        }

        if (parts.Count > 0)
        {
            return string.Join(", ", parts);
        }

        return !string.IsNullOrWhiteSpace(shippingAddress)
            ? shippingAddress.Trim()
            : deliveryStreet?.Trim();
    }
}
