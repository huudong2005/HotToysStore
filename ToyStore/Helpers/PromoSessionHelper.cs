using System.Globalization;
using ToyStore.Domain.Interfaces;
using ToyStore.Models;

namespace ToyStore.Helpers;

/// <summary>
/// Đồng bộ mã khuyến mãi (voucher) từ Session sang giỏ hàng trước khi đặt hàng.
/// </summary>
public static class PromoSessionHelper
{
    public const string AppliedPromoCodeKey = "AppliedPromoCode";
    public const string DiscountValueKey = "DiscountValue";

    public static void ApplySessionPromoToCart(HttpContext context, ShoppingCart cart)
    {
        var promoCode = context.Session.GetString(AppliedPromoCodeKey);
        if (string.IsNullOrWhiteSpace(promoCode))
        {
            return;
        }

        decimal discount = 0m;
        var storedDiscount = context.Session.GetString(DiscountValueKey);
        if (!string.IsNullOrWhiteSpace(storedDiscount))
        {
            decimal.TryParse(storedDiscount, NumberStyles.Any, CultureInfo.InvariantCulture, out discount);
        }

        if (discount <= 0)
        {
            return;
        }

        var subtotal = cart.Subtotal;
        if (discount > subtotal)
        {
            discount = subtotal;
        }

        cart.AppliedPromoCode = promoCode.Trim();
        cart.AppliedPromoDiscount = discount;
    }

    public static void ClearSessionPromo(HttpContext context)
    {
        context.Session.Remove(AppliedPromoCodeKey);
        context.Session.Remove(DiscountValueKey);
    }

    public static async Task RecordPromoUsageAsync(IUnitOfWork unitOfWork, ShoppingCart cart)
    {
        if (string.IsNullOrWhiteSpace(cart.AppliedPromoCode))
        {
            return;
        }

        await unitOfWork.Promotions.IncrementUsedCountAsync(cart.AppliedPromoCode);
    }

    public static void CapDiscountToSubtotal(ShoppingCart cart, HttpContext? context = null)
    {
        if (cart.AppliedPromoDiscount < 0)
        {
            cart.AppliedPromoDiscount = 0;
        }

        if (cart.AppliedPromoDiscount > cart.Subtotal)
        {
            cart.AppliedPromoDiscount = cart.Subtotal;
        }

        if (context != null)
        {
            context.Session.SetString(
                DiscountValueKey,
                cart.AppliedPromoDiscount.ToString(CultureInfo.InvariantCulture));
        }
    }
}
