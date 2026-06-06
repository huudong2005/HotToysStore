using Microsoft.EntityFrameworkCore;
using ToyStore.Domain.Entities;
using ToyStore.Domain.Interfaces;
using ToyStore.Infrastructure.Data;
using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace ToyStore.Infrastructure.Repositories;

public class OrderRepository : GenericRepository<Order>, IOrderRepository
{
    public OrderRepository(ToyStoreContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Order>> GetOrdersByCustomerIdAsync(int customerId)
    {
        return await _dbSet
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
    }

    public async Task<Order?> GetOrderWithDetailsAsync(int orderId)
    {
        return await _dbSet
            .Include(o => o.Customer)
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                    .ThenInclude(p => p.Category)
            .FirstOrDefaultAsync(o => o.OrderId == orderId);
    }

    public async Task<IEnumerable<Order>> GetOrdersByStatusAsync(string status)
    {
        return await _dbSet
            .Where(o => o.Status == status)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
    }

    // ---- TRIỂN KHAI STORED PROCEDURE ----
    public async Task<int> DeleteOrderViaProcedureAsync(int orderId)
    {
        var p_OrderId = new OracleParameter("p_OrderId", OracleDbType.Int32) { Value = orderId };
        var p_ResultCode = new OracleParameter("p_ResultCode", OracleDbType.Int32) { Direction = ParameterDirection.Output };

        string sql = "BEGIN \"SP_DeleteOrder\"(:p_OrderId, :p_ResultCode); END;";
        await _context.Database.ExecuteSqlRawAsync(sql, p_OrderId, p_ResultCode);

        return Convert.ToInt32(p_ResultCode.Value.ToString());
    }

    public async Task<int> CancelOrderAndRestoreStockViaProcedureAsync(int orderId, string status)
    {
        var p_OrderId = new OracleParameter("p_OrderId", OracleDbType.Int32) { Value = orderId };
        var p_Status = new OracleParameter("p_Status", OracleDbType.Varchar2, 50) { Value = status };
        var p_ResultCode = new OracleParameter("p_ResultCode", OracleDbType.Int32) { Direction = ParameterDirection.Output };

        string sql = "BEGIN \"SP_CancelOrder\"(:p_OrderId, :p_Status, :p_ResultCode); END;";
        await _context.Database.ExecuteSqlRawAsync(sql, p_OrderId, p_Status, p_ResultCode);

        return Convert.ToInt32(p_ResultCode.Value.ToString());
    }

    public async Task UpdateOrderStatusViaProcedureAsync(int orderId, string status)
    {
        var p_OrderId = new OracleParameter("p_OrderId", OracleDbType.Int32) { Value = orderId };
        var p_Status = new OracleParameter("p_Status", OracleDbType.Varchar2, 50) { Value = status };

        string sql = "BEGIN \"SP_UpdateOrderStatus\"(:p_OrderId, :p_Status); END;";
        await _context.Database.ExecuteSqlRawAsync(sql, p_OrderId, p_Status);
    }

    public async Task<int> CreateOrderHeaderViaProcedureAsync(
        int customerId,
        decimal totalAmount,
        string paymentMethod,
        string deliveryMethod,
        decimal subtotal,
        decimal discountValue,
        string discountStrategy)
    {
        var p_CustomerId = new OracleParameter("p_CustomerId", OracleDbType.Int32) { Value = customerId };
        var p_TotalAmount = new OracleParameter("p_TotalAmount", OracleDbType.Decimal) { Value = totalAmount };
        var p_PaymentMethod = new OracleParameter("p_PaymentMethod", OracleDbType.Varchar2, 50) { Value = paymentMethod };
        var p_DeliveryMethod = new OracleParameter("p_DeliveryMethod", OracleDbType.Varchar2, 50)
        {
            Value = string.IsNullOrWhiteSpace(deliveryMethod) ? "Standard" : deliveryMethod
        };
        var p_Subtotal = new OracleParameter("p_Subtotal", OracleDbType.Decimal) { Value = subtotal };
        var p_DiscountValue = new OracleParameter("p_DiscountValue", OracleDbType.Decimal) { Value = discountValue };
        var p_DiscountStrategy = new OracleParameter("p_DiscountStrategy", OracleDbType.Varchar2, 50)
        {
            Value = string.IsNullOrWhiteSpace(discountStrategy) ? "NoDiscount" : discountStrategy
        };
        var p_OrderId = new OracleParameter("p_OrderId", OracleDbType.Int32) { Direction = ParameterDirection.Output };

        string sql = "BEGIN \"SP_CreateOrderHeader\"(:p_CustomerId, :p_TotalAmount, :p_PaymentMethod, :p_DeliveryMethod, :p_Subtotal, :p_DiscountValue, :p_DiscountStrategy, :p_OrderId); END;";
        await _context.Database.ExecuteSqlRawAsync(sql, p_CustomerId, p_TotalAmount, p_PaymentMethod, p_DeliveryMethod, p_Subtotal, p_DiscountValue, p_DiscountStrategy, p_OrderId);

        return Convert.ToInt32(p_OrderId.Value.ToString());
    }

    public async Task<int> CreateOrderDetailViaProcedureAsync(int orderId, int productId, int quantity, decimal unitPrice)
    {
        var p_OrderId = new OracleParameter("p_OrderId", OracleDbType.Int32) { Value = orderId };
        var p_ProductId = new OracleParameter("p_ProductId", OracleDbType.Int32) { Value = productId };
        var p_Quantity = new OracleParameter("p_Quantity", OracleDbType.Int32) { Value = quantity };
        var p_UnitPrice = new OracleParameter("p_UnitPrice", OracleDbType.Decimal) { Value = unitPrice };
        var p_ResultCode = new OracleParameter("p_ResultCode", OracleDbType.Int32) { Direction = ParameterDirection.Output };

        string sql = "BEGIN \"SP_CreateOrderDetail\"(:p_OrderId, :p_ProductId, :p_Quantity, :p_UnitPrice, :p_ResultCode); END;";
        await _context.Database.ExecuteSqlRawAsync(sql, p_OrderId, p_ProductId, p_Quantity, p_UnitPrice, p_ResultCode);

        return Convert.ToInt32(p_ResultCode.Value.ToString());
    }
}
