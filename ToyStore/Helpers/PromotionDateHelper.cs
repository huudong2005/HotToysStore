using ToyStore.Domain.Entities;

namespace ToyStore.Helpers;

/// <summary>
/// Chuẩn hóa ngày hiệu lực khi admin gia hạn mã đã hết hạn.
/// </summary>
public static class PromotionDateHelper
{
    public static void NormalizeOnReactivation(Promotion existing, Promotion updated)
    {
        if (!updated.IsActive)
        {
            return;
        }

        var now = DateTime.Now;
        if (updated.EndDate < now)
        {
            return;
        }

        // Mã cũ đã hết hạn, admin gia hạn thêm: kích hoạt ngay thay vì chờ StartDate ở tương lai.
        if (existing.EndDate < now && updated.StartDate > now)
        {
            updated.StartDate = now;
        }
    }
}
