using ToyStore.Domain.Entities;

namespace ToyStore.Domain.Interfaces;

public interface IBannerRepository : IGenericRepository<Banner>
{
    // Lấy TẤT CẢ banner đang kích hoạt để hiển thị dạng carousel ngoài trang chủ.
    Task<IEnumerable<Banner>> GetActiveBannersAsync();
}
