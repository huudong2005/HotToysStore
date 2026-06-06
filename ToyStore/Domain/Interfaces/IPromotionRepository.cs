using ToyStore.Domain.Entities;

namespace ToyStore.Domain.Interfaces;

public interface IPromotionRepository : IGenericRepository<Promotion>
{
    // Kiểm tra trùng mã code (loại trừ chính nó khi đang sửa)
    Task<bool> IsCodeExistsAsync(string promotionCode, int? excludeId = null);

    // Gọi Stored Procedure SP_APPLY_PROMOTION để xác thực & tính tiền giảm cho 1 mã khuyến mãi.
    // ResultCode = 1: thành công; số âm: lỗi. DiscountValue: số tiền giảm. Message: lời nhắn hiển thị.
    Task<(int ResultCode, decimal DiscountValue, string Message)> ApplyPromotionAsync(string promoCode, decimal orderValue);
}
