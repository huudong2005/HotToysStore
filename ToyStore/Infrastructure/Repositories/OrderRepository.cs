using Microsoft.EntityFrameworkCore;
using ToyStore.Domain.Entities;
using ToyStore.Domain.Interfaces;
using ToyStore.Infrastructure.Data;
using ToyStore.Models;
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

    public async Task<IEnumerable<PromotionStatisticViewModel>> GetPromotionStatisticsAsync(DateTime startDate, DateTime endDate)
    {
        const string completedStatus = "Hoàn thành";

        var monthlyStats = await _dbSet
            .Where(o => o.Status == completedStatus
                && o.OrderDate.HasValue
                && o.OrderDate.Value >= startDate
                && o.OrderDate.Value <= endDate)
            .GroupBy(o => new { o.OrderDate!.Value.Year, o.OrderDate!.Value.Month })
            .Select(g => new PromotionStatisticViewModel
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                TotalVoucherDiscount = g.Sum(o => o.DiscountValue),
                TotalMembershipDiscount = g.Sum(o => o.MembershipDiscountValue),
                TotalDiscount = g.Sum(o => o.DiscountValue + o.MembershipDiscountValue),
                ActualRevenue = g.Sum(o => o.TotalAmount)
            })
            .ToListAsync();

        var monthsInRange = new List<(int Year, int Month)>();
        var cursor = new DateTime(startDate.Year, startDate.Month, 1);
        var lastMonth = new DateTime(endDate.Year, endDate.Month, 1);

        while (cursor <= lastMonth)
        {
            monthsInRange.Add((cursor.Year, cursor.Month));
            cursor = cursor.AddMonths(1);
        }

        return monthsInRange
            .Select(ym =>
            {
                var stat = monthlyStats.FirstOrDefault(s => s.Year == ym.Year && s.Month == ym.Month);
                return stat ?? new PromotionStatisticViewModel
                {
                    Year = ym.Year,
                    Month = ym.Month,
                    TotalVoucherDiscount = 0m,
                    TotalMembershipDiscount = 0m,
                    TotalDiscount = 0m,
                    ActualRevenue = 0m
                };
            })
            .OrderBy(s => s.Year)
            .ThenBy(s => s.Month)
            .ToList();
    }

    public async Task<List<VoucherStatisticViewModel>> GetVoucherStatisticsAsync(DateTime startDate, DateTime endDate)
    {
        const string completedStatus = "Hoàn thành";

        var rawOrders = await _dbSet
            .AsNoTracking()
            .Where(o => o.OrderDate.HasValue
                && o.OrderDate.Value >= startDate
                && o.OrderDate.Value <= endDate
                && o.DiscountValue > 0)
            .Select(o => new
            {
                o.Status,
                o.DiscountStrategyName,
                o.DiscountValue
            })
            .ToListAsync();

        var promotionCodes = await _context.Promotions
            .AsNoTracking()
            .Select(p => p.PromotionCode)
            .ToListAsync();

        var knownCodes = new HashSet<string>(promotionCodes, StringComparer.OrdinalIgnoreCase);

        var voucherRows = rawOrders
            .Where(o => string.Equals(o.Status, completedStatus, StringComparison.Ordinal))
            .Select(o =>
            {
                var strategy = o.DiscountStrategyName;
                string code;

                if (!System.String.IsNullOrWhiteSpace(strategy) && knownCodes.Contains(strategy))
                {
                    code = strategy;
                }
                else if (!System.String.IsNullOrWhiteSpace(strategy)
                         && !string.Equals(strategy, "NoDiscount", StringComparison.OrdinalIgnoreCase))
                {
                    code = strategy;
                }
                else
                {
                    code = "Không dùng mã";
                }

                return new
                {
                    PromotionCode = code,
                    o.DiscountValue
                };
            })
            .Where(x => !string.Equals(x.PromotionCode, "Không dùng mã", StringComparison.Ordinal))
            .ToList();

        return voucherRows
            .GroupBy(x => x.PromotionCode, StringComparer.OrdinalIgnoreCase)
            .Select(g => new VoucherStatisticViewModel
            {
                PromotionCode = g.Key,
                UsageCount = g.Count(),
                TotalDiscount = g.Sum(x => x.DiscountValue)
            })
            .OrderByDescending(x => x.TotalDiscount)
            .Take(10)
            .ToList();
    }

    public async Task<List<TierStatisticViewModel>> GetTierStatisticsAsync(DateTime startDate, DateTime endDate)
    {
        const string completedStatus = "Hoàn thành";

        var rawOrders = await (
            from o in _dbSet.AsNoTracking()
            join c in _context.Customers.AsNoTracking() on o.CustomerId equals c.CustomerId
            join t in _context.MembershipTiers.AsNoTracking() on c.TierId equals t.TierId into tierJoin
            from t in tierJoin.DefaultIfEmpty()
            where o.OrderDate.HasValue
                && o.OrderDate.Value >= startDate
                && o.OrderDate.Value <= endDate
                && o.MembershipDiscountValue > 0
            select new
            {
                o.Status,
                TierName = t.TierName,
                o.MembershipDiscountValue
            }).ToListAsync();

        return rawOrders
            .Where(o => string.Equals(o.Status, completedStatus, StringComparison.Ordinal))
            .GroupBy(x => System.String.IsNullOrWhiteSpace(x.TierName) ? "Chưa có hạng" : x.TierName!)
            .Select(g => new TierStatisticViewModel
            {
                TierName = g.Key,
                OrderCount = g.Count(),
                TotalMembershipDiscount = g.Sum(x => x.MembershipDiscountValue)
            })
            .OrderByDescending(x => x.TotalMembershipDiscount)
            .Take(10)
            .ToList();
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
