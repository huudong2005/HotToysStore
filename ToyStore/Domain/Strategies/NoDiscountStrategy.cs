using ToyStore.Domain.Interfaces;

namespace ToyStore.Domain.Strategies;

/// <summary>
/// Strategy: Không có khuyến mãi
/// </summary>
public class NoDiscountStrategy : IDiscountStrategy
{
    public string StrategyName => "NoDiscount";

    public decimal CalculateDiscount(decimal total)
    {
        // Không giảm giá
        return 0;
    }
}
