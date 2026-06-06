using Microsoft.EntityFrameworkCore;
using ToyStore.Domain.Entities;
using ToyStore.Domain.Interfaces;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Infrastructure.Repositories;

public class BannerRepository : GenericRepository<Banner>, IBannerRepository
{
    public BannerRepository(ToyStoreContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Banner>> GetActiveBannersAsync()
    {
        // So sánh tường minh "== true" cho cột NUMBER(1) để Oracle dịch thành "IsActive = 1".
        // Lấy tất cả banner đang Active, banner mới nhất hiển thị trước.
        return await _dbSet
            .Where(b => b.IsActive == true)
            .OrderByDescending(b => b.BannerId)
            .ToListAsync();
    }
}
