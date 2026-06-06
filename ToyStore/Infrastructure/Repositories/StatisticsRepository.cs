using System.Data;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using ToyStore.Domain.Interfaces;
using ToyStore.Models;

namespace ToyStore.Infrastructure.Repositories;

/// <summary>
/// Đọc dữ liệu thống kê doanh thu từ Oracle qua RefCursor (SYS_REFCURSOR).
/// </summary>
public class StatisticsRepository : IStatisticsRepository
{
    private readonly string _connectionString;

    public StatisticsRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("ToyStoreDB")
            ?? throw new InvalidOperationException("Connection string 'ToyStoreDB' not found.");
    }

    public Task<List<RevenueViewModel>> GetRevenueByMonthAsync(int year)
    {
        return ExecuteRevenueProcedureAsync(
            procedureCall: "BEGIN \"SP_REVENUEBYMONTH\"(:p_Year, :p_cursor); END;",
            configureInput: command =>
            {
                command.Parameters.Add(new OracleParameter("p_Year", OracleDbType.Int32)
                {
                    Value = year
                });
            },
            mapRow: reader =>
            {
                var month = GetInt32(reader, "Month");
                var rowYear = GetInt32(reader, "Year");
                return new RevenueViewModel
                {
                    Label = $"Tháng {month}/{rowYear}",
                    Year = rowYear,
                    TotalOrders = GetInt32(reader, "TotalOrders"),
                    TotalRevenue = GetDecimal(reader, "TotalRevenue")
                };
            });
    }

    public Task<List<RevenueViewModel>> GetRevenueByQuarterAsync(int year)
    {
        return ExecuteRevenueProcedureAsync(
            procedureCall: "BEGIN \"SP_REVENUE_BY_QUARTER\"(:p_Year, :p_cursor); END;",
            configureInput: command =>
            {
                command.Parameters.Add(new OracleParameter("p_Year", OracleDbType.Int32)
                {
                    Value = year
                });
            },
            mapRow: reader =>
            {
                var quarter = GetInt32(reader, "Quarter");
                var rowYear = GetInt32(reader, "Year");
                return new RevenueViewModel
                {
                    Label = $"Quý {quarter}/{rowYear}",
                    Year = rowYear,
                    TotalOrders = GetInt32(reader, "TotalOrders"),
                    TotalRevenue = GetDecimal(reader, "TotalRevenue")
                };
            });
    }

    public Task<List<RevenueViewModel>> GetRevenueByYearAsync()
    {
        return ExecuteRevenueProcedureAsync(
            procedureCall: "BEGIN \"SP_REVENUE_BY_YEAR\"(:p_cursor); END;",
            configureInput: null,
            mapRow: reader =>
            {
                var rowYear = GetInt32(reader, "Year");
                return new RevenueViewModel
                {
                    Label = rowYear.ToString(),
                    Year = rowYear,
                    TotalOrders = GetInt32(reader, "TotalOrders"),
                    TotalRevenue = GetDecimal(reader, "TotalRevenue")
                };
            });
    }

    public Task<List<TopProductViewModel>> GetTopSellingProductsAsync(int topN)
    {
        return ExecuteRefCursorProcedureAsync(
            procedureCall: "BEGIN \"SP_TOP_SELLING_PRODUCTS\"(:p_TopN, :p_cursor); END;",
            configureInput: command =>
            {
                command.Parameters.Add(new OracleParameter("p_TopN", OracleDbType.Int32)
                {
                    Value = topN
                });
            },
            mapRow: reader => new TopProductViewModel
            {
                ProductID = GetInt32(reader, "ProductID"),
                ProductName = GetString(reader, "ProductName"),
                TotalSold = GetInt32(reader, "TotalSold"),
                Revenue = GetDecimal(reader, "Revenue")
            });
    }

    public Task<List<TopCustomerViewModel>> GetTopCustomersAsync(int topN)
    {
        return ExecuteRefCursorProcedureAsync(
            procedureCall: "BEGIN \"SP_TOP_CUSTOMERS\"(:p_TopN, :p_cursor); END;",
            configureInput: command =>
            {
                command.Parameters.Add(new OracleParameter("p_TopN", OracleDbType.Int32)
                {
                    Value = topN
                });
            },
            mapRow: reader => new TopCustomerViewModel
            {
                CustomerID = GetInt32(reader, "CustomerID"),
                FullName = GetString(reader, "FullName"),
                Email = GetString(reader, "Email"),
                TotalOrders = GetInt32(reader, "TotalOrders"),
                TotalSpent = GetDecimal(reader, "TotalSpent")
            });
    }

    /// <summary>
    /// Thực thi PL/SQL block, đọc RefCursor bằng OracleDataReader và map sang danh sách ViewModel.
    /// </summary>
    private Task<List<RevenueViewModel>> ExecuteRevenueProcedureAsync(
        string procedureCall,
        Action<OracleCommand>? configureInput,
        Func<OracleDataReader, RevenueViewModel> mapRow)
    {
        return ExecuteRefCursorProcedureAsync(procedureCall, configureInput, mapRow);
    }

    private async Task<List<T>> ExecuteRefCursorProcedureAsync<T>(
        string procedureCall,
        Action<OracleCommand>? configureInput,
        Func<OracleDataReader, T> mapRow)
    {
        var results = new List<T>();

        await using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.BindByName = true;
        command.CommandType = CommandType.Text;
        command.CommandText = procedureCall;

        configureInput?.Invoke(command);

        var cursorParam = new OracleParameter("p_cursor", OracleDbType.RefCursor)
        {
            Direction = ParameterDirection.Output
        };
        command.Parameters.Add(cursorParam);

        await command.ExecuteNonQueryAsync();

        if (cursorParam.Value is not OracleRefCursor refCursor)
        {
            return results;
        }

        await using var reader = refCursor.GetDataReader();
        while (await reader.ReadAsync())
        {
            results.Add(mapRow(reader));
        }

        return results;
    }

    private static int GetColumnOrdinal(OracleDataReader reader, string columnName)
    {
        for (var i = 0; i < reader.FieldCount; i++)
        {
            if (string.Equals(reader.GetName(i), columnName, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        throw new InvalidOperationException($"Column '{columnName}' was not found in the result set.");
    }

    private static int GetInt32(OracleDataReader reader, string columnName)
    {
        var ordinal = GetColumnOrdinal(reader, columnName);
        if (reader.IsDBNull(ordinal))
        {
            return 0;
        }

        return Convert.ToInt32(reader.GetValue(ordinal));
    }

    private static decimal GetDecimal(OracleDataReader reader, string columnName)
    {
        var ordinal = GetColumnOrdinal(reader, columnName);
        if (reader.IsDBNull(ordinal))
        {
            return 0m;
        }

        return Convert.ToDecimal(reader.GetValue(ordinal));
    }

    private static string GetString(OracleDataReader reader, string columnName)
    {
        var ordinal = GetColumnOrdinal(reader, columnName);
        if (reader.IsDBNull(ordinal))
        {
            return string.Empty;
        }

        return reader.GetString(ordinal);
    }
}
