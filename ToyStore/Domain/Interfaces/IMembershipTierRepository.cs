using ToyStore.Domain.Entities;

namespace ToyStore.Domain.Interfaces;

public interface IMembershipTierRepository : IGenericRepository<MembershipTier>
{
    /// <summary>
    /// Lấy toàn bộ hạng thẻ thành viên, xếp theo RequiredOrders giảm dần
    /// (hạng cao - yêu cầu nhiều đơn nhất - đứng đầu danh sách).
    /// </summary>
    Task<List<MembershipTier>> GetAllOrderedByRequiredOrdersDescAsync();
}
