using Microsoft.EntityFrameworkCore;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Services;

/// <summary>
/// Gợi ý sản phẩm dựa trên thuật toán Apriori (phiên bản rút gọn cho đồ án).
/// Khai thác bảng UserActivityLog — mỗi "giao dịch" = tập sản phẩm khách quan tâm trong cùng một ngày.
/// </summary>
public class RecommendationService : IRecommendationService
{
    /// <summary>
    /// Ngưỡng hỗ trợ tối thiểu: cặp (currentProduct, B) phải xuất hiện chung ít nhất N giao dịch.
    /// </summary>
    private const int MinSupportCount = 2;

    private static readonly string[] RelevantActivityTypes = { "ViewProduct", "AddToCart" };

    private readonly ToyStoreContext _context;

    public RecommendationService(ToyStoreContext context)
    {
        _context = context;
    }

    public async Task<List<int>> GetRecommendedProductIdsAsync(int currentProductId, int limit = 4)
    {
        if (currentProductId <= 0 || limit <= 0)
        {
            return new List<int>();
        }

        // ── Bước 1: Chuẩn bị tập giao dịch (Transactions) từ UserActivityLog ──
        // Mỗi giao dịch = danh sách ProductID duy nhất mà cùng một Customer
        // thực hiện hành vi ViewProduct / AddToCart trong cùng một ngày.
        var transactions = await BuildTransactionsAsync();

        if (transactions.Count == 0)
        {
            return new List<int>();
        }

        // ── Bước 2: Apriori rút gọn — chỉ sinh tập phổ biến kích thước 2 (cặp sản phẩm) ──
        // Lọc các giao dịch có chứa sản phẩm đang xem (itemset gốc A = currentProductId).
        var transactionsContainingCurrent = transactions
            .Where(t => t.Contains(currentProductId))
            .ToList();

        if (transactionsContainingCurrent.Count == 0)
        {
            return new List<int>();
        }

        // support(A) = số giao dịch chứa A (dùng làm mẫu số cho confidence).
        var supportA = transactionsContainingCurrent.Count;

        // Đếm support(A ∪ B): số giao dịch chứa đồng thời A và từng sản phẩm B khác.
        var pairSupportCounts = new Dictionary<int, int>();

        foreach (var transaction in transactionsContainingCurrent)
        {
            foreach (var productId in transaction)
            {
                if (productId == currentProductId)
                {
                    continue;
                }

                pairSupportCounts.TryGetValue(productId, out var count);
                pairSupportCounts[productId] = count + 1;
            }
        }

        // Lọc theo MinSupport, tính confidence(B|A) = support(A∪B) / support(A), sắp xếp giảm dần.
        var rankedRecommendations = pairSupportCounts
            .Where(kv => kv.Value >= MinSupportCount)
            .Select(kv => new
            {
                ProductId = kv.Key,
                SupportCount = kv.Value,
                Confidence = (double)kv.Value / supportA
            })
            .OrderByDescending(x => x.Confidence)
            .ThenByDescending(x => x.SupportCount)
            .Take(limit)
            .Select(x => x.ProductId)
            .ToList();

        return rankedRecommendations;
    }

    /// <summary>
    /// Truy vấn log hành vi và gom thành tập giao dịch in-memory (mỗi phần tử là HashSet ProductID).
    /// </summary>
    private async Task<List<HashSet<int>>> BuildTransactionsAsync()
    {
        var rawLogs = await _context.UserActivityLogs
            .AsNoTracking()
            .Where(log =>
                RelevantActivityTypes.Contains(log.ActivityType)
                && log.ProductId != null
                && log.ProductId > 0)
            .Select(log => new
            {
                log.CustomerId,
                ProductId = log.ProductId!.Value,
                SessionDate = log.Timestamp.Date
            })
            .ToListAsync();

        // GroupBy (CustomerID, Ngày) → một "phiên mua sắm" / "phiên duyệt web".
        return rawLogs
            .GroupBy(x => new { x.CustomerId, x.SessionDate })
            .Select(group =>
            {
                var itemset = new HashSet<int>();
                foreach (var entry in group)
                {
                    itemset.Add(entry.ProductId);
                }

                return itemset;
            })
            .Where(set => set.Count > 0)
            .ToList();
    }
}
