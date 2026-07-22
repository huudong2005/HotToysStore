namespace ToyStore.Services;

public interface IRecommendationService
{
    /// <summary>
    /// Gợi ý sản phẩm liên quan bằng Apriori (tập phổ biến kích thước 2) từ UserActivityLog.
    /// </summary>
    Task<List<int>> GetRecommendedProductIdsAsync(int currentProductId, int limit = 4);
}
