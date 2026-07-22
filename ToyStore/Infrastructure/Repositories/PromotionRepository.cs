using System.Data;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;
using ToyStore.Domain.Entities;
using ToyStore.Domain.Interfaces;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Infrastructure.Repositories;

public class PromotionRepository : GenericRepository<Promotion>, IPromotionRepository
{
    public PromotionRepository(ToyStoreContext context) : base(context)
    {
    }

    public async Task<(int ResultCode, decimal DiscountValue, string Message)> ApplyPromotionAsync(string promoCode, decimal orderValue)
    {
        // Dùng ADO.NET thuần (OracleCommand) trên connection của DbContext.
        var connection = (OracleConnection)_context.Database.GetDbConnection();
        bool openedHere = connection.State != ConnectionState.Open;

        try
        {
            if (openedHere)
            {
                await connection.OpenAsync();
            }

            using var command = (OracleCommand)connection.CreateCommand();
            command.CommandText = "SP_APPLY_PROMOTION";
            command.CommandType = CommandType.StoredProcedure;
            command.BindByName = true; // Bind theo tên tham số cho an toàn

            // Tham số IN
            command.Parameters.Add(new OracleParameter("p_Code", OracleDbType.Varchar2, 50)
            {
                Direction = ParameterDirection.Input,
                Value = string.IsNullOrWhiteSpace(promoCode) ? (object)DBNull.Value : promoCode.Trim()
            });
            command.Parameters.Add(new OracleParameter("p_OrderValue", OracleDbType.Decimal)
            {
                Direction = ParameterDirection.Input,
                Value = orderValue
            });

            // Tham số OUT
            var pResultCode = new OracleParameter("p_ResultCode", OracleDbType.Int32) { Direction = ParameterDirection.Output };
            var pDiscountValue = new OracleParameter("p_DiscountValue", OracleDbType.Decimal) { Direction = ParameterDirection.Output };
            var pMessage = new OracleParameter("p_Message", OracleDbType.Varchar2, 255) { Direction = ParameterDirection.Output };

            command.Parameters.Add(pResultCode);
            command.Parameters.Add(pDiscountValue);
            command.Parameters.Add(pMessage);

            await command.ExecuteNonQueryAsync();

            int resultCode = ToInt(pResultCode.Value, -99);
            decimal discountValue = ToDecimal(pDiscountValue.Value);
            string message = pMessage.Value == null || pMessage.Value is DBNull
                ? string.Empty
                : pMessage.Value.ToString()!;

            return (resultCode, discountValue, message);
        }
        finally
        {
            // Chỉ đóng connection nếu chính hàm này mở nó ra.
            if (openedHere && connection.State == ConnectionState.Open)
            {
                await connection.CloseAsync();
            }
        }
    }

    // Giá trị OUT của ODP.NET là OracleDecimal -> parse an toàn qua chuỗi (InvariantCulture).
    private static int ToInt(object? value, int fallback)
    {
        if (value == null || value is DBNull) return fallback;
        return int.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var result)
            ? result
            : fallback;
    }

    private static decimal ToDecimal(object? value)
    {
        if (value == null || value is DBNull) return 0m;
        return decimal.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var result)
            ? result
            : 0m;
    }

    public async Task<IEnumerable<Promotion>> GetActivePromotionsAsync()
    {
        // Lấy toàn bộ rồi lọc in-memory để tránh lệch so sánh TIMESTAMP/IsActive trên Oracle EF.
        var promotions = await _dbSet
            .AsNoTracking()
            .ToListAsync();

        var now = DateTime.Now;

        return promotions
            .Where(p => p.IsActive
                        && p.StartDate <= now
                        && p.EndDate >= now
                        && (p.UsageLimit == 0 || p.UsedCount < p.UsageLimit))
            .OrderBy(p => p.MinOrderValue)
            .ThenBy(p => p.EndDate)
            .ToList();
    }

    public async Task<bool> IncrementUsedCountAsync(string promotionCode)
    {
        if (string.IsNullOrWhiteSpace(promotionCode))
        {
            return false;
        }

        var normalizedCode = promotionCode.Trim();
        var promo = await _dbSet
            .FirstOrDefaultAsync(p => p.PromotionCode == normalizedCode);

        if (promo == null)
        {
            return false;
        }

        if (promo.UsageLimit > 0 && promo.UsedCount >= promo.UsageLimit)
        {
            return false;
        }

        promo.UsedCount += 1;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> IsCodeExistsAsync(string promotionCode, int? excludeId = null)
    {
        if (string.IsNullOrWhiteSpace(promotionCode))
        {
            return false;
        }

        var query = _dbSet.Where(p => p.PromotionCode == promotionCode);

        if (excludeId.HasValue)
        {
            query = query.Where(p => p.PromotionId != excludeId.Value);
        }

        return await query.AnyAsync();
    }
}
