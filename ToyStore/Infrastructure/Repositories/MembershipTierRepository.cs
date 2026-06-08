using Microsoft.EntityFrameworkCore;
using ToyStore.Domain.Entities;
using ToyStore.Domain.Interfaces;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Infrastructure.Repositories;

public class MembershipTierRepository : GenericRepository<MembershipTier>, IMembershipTierRepository
{
    public MembershipTierRepository(ToyStoreContext context) : base(context)
    {
    }

    public async Task<List<MembershipTier>> GetAllOrderedByRequiredOrdersDescAsync()
    {
        return await _dbSet
            .OrderByDescending(t => t.RequiredOrders)
            .ToListAsync();
    }
}
