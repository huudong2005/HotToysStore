namespace ToyStore.Domain.Interfaces;

/// <summary>
/// Interface định nghĩa Strategy cho việc tính toán khuyến mãi
/// </summary>
public interface IDiscountStrategy
{
    /// <summary>
    /// Tên của strategy (VipDiscount, SeasonalDiscount, NoDiscount)
    /// </summary>
    string StrategyName { get; }

    /// <summary>
    /// Tính toán giá trị khuyến mãi
    /// </summary>
    /// <param name="total">Tổng tiền trước khi giảm giá (∑(Quantity × UnitPrice))</param>
    /// <returns>Giá trị khuyến mãi (DiscountValue)</returns>
    decimal CalculateDiscount(decimal total);

    /// <summary>
    /// Tính tổng tiền sau khi áp dụng khuyến mãi
    /// Áp dụng công thức: Total = ∑(Quantity × UnitPrice) – DiscountValue
    /// </summary>
    /// <param name="total">Tổng tiền trước khi giảm giá</param>
    /// <returns>Tổng tiền sau khi giảm giá</returns>
    decimal CalculateTotal(decimal total)
    {
        var discount = CalculateDiscount(total);
        var finalTotal = total - discount;
        return finalTotal < 0 ? 0 : finalTotal; // Đảm bảo không âm
    }
}
