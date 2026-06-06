using Microsoft.EntityFrameworkCore;
using ToyStore.Domain.Entities;
using ToyStore.Domain.Interfaces;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Infrastructure.Repositories;

public class OrderDetailRepository : GenericRepository<OrderDetail>, IOrderDetailRepository
{
    public OrderDetailRepository(ToyStoreContext context) : base(context)
    {
    }

    public async Task<IEnumerable<OrderDetail>> GetOrderDetailsByOrderIdAsync(int orderId)
    {
        return await _dbSet
            .Where(od => od.OrderId == orderId)
            .Include(od => od.Product)
            .ToListAsync();
    }
}
