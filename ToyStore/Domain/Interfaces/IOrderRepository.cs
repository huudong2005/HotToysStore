using ToyStore.Domain.Entities;
using ToyStore.Models;

namespace ToyStore.Domain.Interfaces;

public interface IOrderRepository : IGenericRepository<Order>
{
    Task<IEnumerable<Order>> GetOrdersByCustomerIdAsync(int customerId);
    Task<Order?> GetOrderWithDetailsAsync(int orderId);
    Task<IEnumerable<Order>> GetOrdersByStatusAsync(string status);
    Task<IEnumerable<PromotionStatisticViewModel>> GetPromotionStatisticsAsync(DateTime startDate, DateTime endDate);

    // Thêm mới cho Oracle Stored Procedure
    Task<int> DeleteOrderViaProcedureAsync(int orderId);
    Task<int> CancelOrderAndRestoreStockViaProcedureAsync(int orderId, string status);
    Task UpdateOrderStatusViaProcedureAsync(int orderId, string status);
    Task<int> CreateOrderHeaderViaProcedureAsync(
        int customerId,
        decimal totalAmount,
        string paymentMethod,
        string deliveryMethod,
        decimal subtotal,
        decimal discountValue,
        string discountStrategy);
    Task<int> CreateOrderDetailViaProcedureAsync(int orderId, int productId, int quantity, decimal unitPrice);
}
