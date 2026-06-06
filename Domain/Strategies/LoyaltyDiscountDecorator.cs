using ToyStore.Domain.Interfaces;

namespace ToyStore.Domain.Strategies;

/// <summary>
/// Decorator Pattern: Bọc quanh một IDiscountStrategy để cộng thêm ưu đãi khách hàng thân thiết (loyalty bonus)
/// mà không cần sửa logic bên trong strategy gốc.
/// </summary>
public class LoyaltyDiscountDecorator : IDiscountStrategy
{
    private readonly IDiscountStrategy _inner;

    /// <summary>
    /// Tên strategy thể hiện rõ đang dùng decorator
    /// </summary>
    public string StrategyName => $"{_inner.StrategyName}+Loyalty";

    public LoyaltyDiscountDecorator(IDiscountStrategy inner)
    {
        _inner = inner;
    }

    /// <summary>
    /// Tính discount = discount gốc + bonus 1% trên phần tiền còn lại sau khi giảm giá
    /// </summary>
    public decimal CalculateDiscount(decimal total)
    {
        var baseDiscount = _inner.CalculateDiscount(total);
        var baseTotal = total - baseDiscount;
        if (baseTotal < 0)
        {
            baseTotal = 0;
        }

        // Bonus thêm 1% cho khách hàng thân thiết
        var loyaltyBonus = baseTotal * 0.01m;

        return baseDiscount + loyaltyBonus;
    }

    /// <summary>
    /// Tổng tiền sau khi giảm giá (đảm bảo không âm)
    /// </summary>
    public decimal CalculateTotal(decimal total)
    {
        var discount = CalculateDiscount(total);
        var finalTotal = total - discount;
        return finalTotal < 0 ? 0 : finalTotal;
    }
}

