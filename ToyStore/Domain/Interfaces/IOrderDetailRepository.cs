using ToyStore.Domain.Entities;

namespace ToyStore.Domain.Interfaces;

public interface IOrderDetailRepository : IGenericRepository<OrderDetail>
{
    Task<IEnumerable<OrderDetail>> GetOrderDetailsByOrderIdAsync(int orderId);
}
