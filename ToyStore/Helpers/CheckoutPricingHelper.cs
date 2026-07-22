using ToyStore.Models;
using ToyStore.Services;

namespace ToyStore.Helpers;

/// <summary>
/// Tính giá checkout: ưu tiên voucher Session, fallback Strategy Pattern.
/// </summary>
public static class CheckoutPricingHelper
{
    public static (decimal Subtotal, decimal DiscountValue, decimal FinalTotal, string DiscountStrategyName) Calculate(
        ShoppingCart cart,
        DiscountService discountService)
    {
        decimal subtotal = cart.Subtotal;

        if (!string.IsNullOrWhiteSpace(cart.AppliedPromoCode) && cart.AppliedPromoDiscount > 0)
        {
            var voucherDiscount = cart.AppliedPromoDiscount;
            if (voucherDiscount > subtotal)
            {
                voucherDiscount = subtotal;
            }

            return (subtotal, voucherDiscount, subtotal - voucherDiscount, cart.AppliedPromoCode.Trim());
        }

        var (discountValue, finalTotal) = discountService.CalculateDiscountAndTotal(
            subtotal,
            cart.DiscountStrategyName);

        var strategyName = string.IsNullOrWhiteSpace(cart.DiscountStrategyName)
            ? "NoDiscount"
            : cart.DiscountStrategyName!;

        return (subtotal, discountValue, finalTotal, strategyName);
    }
}
