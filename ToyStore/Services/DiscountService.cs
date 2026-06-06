using ToyStore.Domain.Interfaces;
using ToyStore.Domain.Strategies;

namespace ToyStore.Services;

/// <summary>
/// Service để tính toán khuyến mãi và tổng tiền
/// Áp dụng công thức: Total = ∑(Quantity × UnitPrice) – DiscountValue
/// </summary>
public class DiscountService
{
    /// <summary>
    /// Tính toán discount và total với strategy được chỉ định
    /// </summary>
    /// <param name="subtotal">Tổng tiền trước khi giảm giá (∑(Quantity × UnitPrice))</param>
    /// <param name="strategyName">Tên strategy (VipDiscount, SeasonalDiscount, NoDiscount)</param>
    /// <returns>Tuple chứa (DiscountValue, FinalTotal)</returns>
    public (decimal DiscountValue, decimal FinalTotal) CalculateDiscountAndTotal(decimal subtotal, string? strategyName = null)
    {
        var strategy = DiscountStrategyFactory.CreateStrategy(strategyName);
        var discountValue = strategy.CalculateDiscount(subtotal);
        var finalTotal = strategy.CalculateTotal(subtotal);

        return (discountValue, finalTotal);
    }

    /// <summary>
    /// Tính toán discount và total với strategy object
    /// </summary>
    /// <param name="subtotal">Tổng tiền trước khi giảm giá</param>
    /// <param name="strategy">Discount strategy</param>
    /// <returns>Tuple chứa (DiscountValue, FinalTotal)</returns>
    public (decimal DiscountValue, decimal FinalTotal) CalculateDiscountAndTotal(decimal subtotal, IDiscountStrategy strategy)
    {
        var discountValue = strategy.CalculateDiscount(subtotal);
        var finalTotal = strategy.CalculateTotal(subtotal);

        return (discountValue, finalTotal);
    }

    /// <summary>
    /// Lấy tất cả các strategies có sẵn
    /// </summary>
    public IEnumerable<IDiscountStrategy> GetAllStrategies()
    {
        return DiscountStrategyFactory.GetAllStrategies();
    }
}
