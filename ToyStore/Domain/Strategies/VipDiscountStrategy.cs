using ToyStore.Domain.Interfaces;

namespace ToyStore.Domain.Strategies;

/// <summary>
/// Strategy: Giảm giá 10% cho khách hàng VIP
/// </summary>
public class VipDiscountStrategy : IDiscountStrategy
{
    public string StrategyName => "VipDiscount";

    private const decimal DISCOUNT_PERCENTAGE = 0.10m; // 10%

    public decimal CalculateDiscount(decimal total)
    {
        if (total <= 0)
            return 0;

        // Giảm 10% của tổng tiền
        return total * DISCOUNT_PERCENTAGE;
    }
}
