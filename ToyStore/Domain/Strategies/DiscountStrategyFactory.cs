using ToyStore.Domain.Interfaces;

namespace ToyStore.Domain.Strategies;

/// <summary>
/// Factory class để tạo DiscountStrategy từ tên strategy
/// </summary>
public static class DiscountStrategyFactory
{
    /// <summary>
    /// Tạo IDiscountStrategy từ tên strategy.
    /// Hỗ trợ thêm strategy sử dụng Decorator Pattern: VipWithLoyalty.
    /// </summary>
    /// <param name="strategyName">
    /// Tên strategy:
    /// - NoDiscount
    /// - VipDiscount
    /// - SeasonalDiscount
    /// - VipWithLoyalty (VipDiscount được bọc bởi LoyaltyDiscountDecorator)
    /// </param>
    /// <returns>IDiscountStrategy tương ứng</returns>
    public static IDiscountStrategy CreateStrategy(string? strategyName)
    {
        return (strategyName?.Trim() ?? "NoDiscount") switch
        {
            "VipDiscount" => new VipDiscountStrategy(),
            "SeasonalDiscount" => new SeasonalDiscountStrategy(),
            "VipWithLoyalty" => new LoyaltyDiscountDecorator(new VipDiscountStrategy()),
            // Backward/compat: một số nơi có thể submit theo format hiển thị "VipDiscount+Loyalty"
            "VipDiscount+Loyalty" => new LoyaltyDiscountDecorator(new VipDiscountStrategy()),
            "NoDiscount" => new NoDiscountStrategy(),
            _ => new NoDiscountStrategy() // Default to NoDiscount if unknown
        };
    }

    /// <summary>
    /// Lấy danh sách tất cả các strategies có sẵn (bao gồm cả strategy sử dụng Decorator Pattern)
    /// </summary>
    public static IEnumerable<IDiscountStrategy> GetAllStrategies()
    {
        return new IDiscountStrategy[]
        {
            new NoDiscountStrategy(),
            new VipDiscountStrategy(),
            new SeasonalDiscountStrategy(),
            // Decorator Pattern: VipDiscount được bọc bởi LoyaltyDiscountDecorator
            new LoyaltyDiscountDecorator(new VipDiscountStrategy())
        };
    }
}

