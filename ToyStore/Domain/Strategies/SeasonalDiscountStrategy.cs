using ToyStore.Domain.Interfaces;

namespace ToyStore.Domain.Strategies;

/// <summary>
/// Strategy: Giảm giá cố định 50,000 VNĐ cho khuyến mãi theo mùa
/// </summary>
public class SeasonalDiscountStrategy : IDiscountStrategy
{
    public string StrategyName => "SeasonalDiscount";

    private const decimal DISCOUNT_AMOUNT = 50000m; // 50,000 VNĐ

    public decimal CalculateDiscount(decimal total)
    {
        if (total <= 0)
            return 0;

        // Giảm cố định 50,000 VNĐ, nhưng không vượt quá tổng tiền
        return total >= DISCOUNT_AMOUNT ? DISCOUNT_AMOUNT : total;
    }
}
