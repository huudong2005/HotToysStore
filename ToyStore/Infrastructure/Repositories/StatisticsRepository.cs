using Microsoft.EntityFrameworkCore;
using ToyStore.Domain.Interfaces;
using ToyStore.Infrastructure.Data;
using ToyStore.Models;

namespace ToyStore.Infrastructure.Repositories;

/// <summary>
/// Thống kê doanh thu qua EF Core (Oracle) — không phụ thuộc SP có thể chưa tạo trên DB.
/// </summary>
public class StatisticsRepository : IStatisticsRepository
{
    private static readonly string[] CancelledStatuses =
    {
        "Cancelled", "Canceled", "Đã hủy"
    };

    private readonly ToyStoreContext _context;

    public StatisticsRepository(ToyStoreContext context)
    {
        _context = context;
    }

    private IQueryable<Domain.Entities.Order> CountableOrders =>
        _context.Orders.AsNoTracking()
            .Where(o => o.OrderDate != null
                        && (o.Status == null || !CancelledStatuses.Contains(o.Status)));

    public async Task<List<RevenueViewModel>> GetRevenueByMonthAsync(int year)
    {
        var start = new DateTime(year, 1, 1);
        var end = new DateTime(year, 12, 31, 23, 59, 59);

        var monthly = await CountableOrders
            .Where(o => o.OrderDate >= start && o.OrderDate <= end)
            .GroupBy(o => o.OrderDate!.Value.Month)
            .Select(g => new
            {
                Month = g.Key,
                TotalOrders = g.Count(),
                TotalRevenue = g.Sum(o => o.TotalAmount)
            })
            .ToListAsync();

        return Enumerable.Range(1, 12)
            .Select(month =>
            {
                var row = monthly.FirstOrDefault(x => x.Month == month);
                return new RevenueViewModel
                {
                    Label = month.ToString(),
                    Year = year,
                    TotalOrders = row?.TotalOrders ?? 0,
                    TotalRevenue = row?.TotalRevenue ?? 0
                };
            })
            .ToList();
    }

    public async Task<List<RevenueViewModel>> GetRevenueByQuarterAsync(int year)
    {
        var start = new DateTime(year, 1, 1);
        var end = new DateTime(year, 12, 31, 23, 59, 59);

        var quarterly = await CountableOrders
            .Where(o => o.OrderDate >= start && o.OrderDate <= end)
            .GroupBy(o => (o.OrderDate!.Value.Month - 1) / 3 + 1)
            .Select(g => new
            {
                Quarter = g.Key,
                TotalOrders = g.Count(),
                TotalRevenue = g.Sum(o => o.TotalAmount)
            })
            .ToListAsync();

        return Enumerable.Range(1, 4)
            .Select(quarter =>
            {
                var row = quarterly.FirstOrDefault(x => x.Quarter == quarter);
                return new RevenueViewModel
                {
                    Label = $"Quý {quarter}/{year}",
                    Year = year,
                    TotalOrders = row?.TotalOrders ?? 0,
                    TotalRevenue = row?.TotalRevenue ?? 0
                };
            })
            .ToList();
    }

    public async Task<List<RevenueViewModel>> GetRevenueByYearAsync()
    {
        var yearly = await CountableOrders
            .GroupBy(o => o.OrderDate!.Value.Year)
            .Select(g => new RevenueViewModel
            {
                Label = g.Key.ToString(),
                Year = g.Key,
                TotalOrders = g.Count(),
                TotalRevenue = g.Sum(o => o.TotalAmount)
            })
            .OrderBy(x => x.Year)
            .ToListAsync();

        return yearly;
    }

    public async Task<List<TopProductViewModel>> GetTopSellingProductsAsync(int topN)
    {
        return await (
                from od in _context.OrderDetails.AsNoTracking()
                join o in _context.Orders.AsNoTracking() on od.OrderId equals o.OrderId
                join p in _context.Products.AsNoTracking() on od.ProductId equals p.ProductId
                where o.OrderDate != null
                      && (o.Status == null || !CancelledStatuses.Contains(o.Status))
                group od by new { od.ProductId, p.ProductName } into g
                orderby g.Sum(x => x.Quantity) descending
                select new TopProductViewModel
                {
                    ProductID = g.Key.ProductId,
                    ProductName = g.Key.ProductName,
                    TotalSold = g.Sum(x => x.Quantity),
                    Revenue = g.Sum(x => x.Quantity * x.UnitPrice)
                })
            .Take(topN)
            .ToListAsync();
    }

    public async Task<List<TopCustomerViewModel>> GetTopCustomersAsync(int topN)
    {
        return await (
                from o in CountableOrders
                join c in _context.Customers.AsNoTracking() on o.CustomerId equals c.CustomerId
                group o by new { c.CustomerId, c.FullName, c.Email } into g
                orderby g.Sum(x => x.TotalAmount) descending
                select new TopCustomerViewModel
                {
                    CustomerID = g.Key.CustomerId,
                    FullName = g.Key.FullName,
                    Email = g.Key.Email,
                    TotalOrders = g.Count(),
                    TotalSpent = g.Sum(x => x.TotalAmount)
                })
            .Take(topN)
            .ToListAsync();
    }
}
