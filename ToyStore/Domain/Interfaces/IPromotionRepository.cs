using ToyStore.Domain.Entities;

namespace ToyStore.Domain.Interfaces;

public interface IPromotionRepository : IGenericRepository<Promotion>
{
    // Kiểm tra trùng mã code (loại trừ chính nó khi đang sửa)
    Task<bool> IsCodeExistsAsync(string promotionCode, int? excludeId = null);

    // Gọi Stored Procedure SP_APPLY_PROMOTION để xác thực & tính tiền giảm cho 1 mã khuyến mãi.
    // ResultCode = 1: thành công; số âm: lỗi. DiscountValue: số tiền giảm. Message: lời nhắn hiển thị.
    Task<(int ResultCode, decimal DiscountValue, string Message)> ApplyPromotionAsync(string promoCode, decimal orderValue);

    // Lấy danh sách mã khuyến mãi đang hoạt động: IsActive = true, còn lượt dùng (UsageLimit = 0 nghĩa là không giới hạn),
    // và còn hiệu lực theo thời gian (StartDate <= now <= EndDate).
    Task<IEnumerable<Promotion>> GetActivePromotionsAsync();

    /// <summary>
    /// Tăng số lượt đã dùng khi đơn hàng được đặt thành công với mã khuyến mãi.
    /// </summary>
    Task<bool> IncrementUsedCountAsync(string promotionCode);
}
