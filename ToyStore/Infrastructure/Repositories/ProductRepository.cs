using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using ToyStore.Domain.Entities;
using ToyStore.Domain.Interfaces;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Infrastructure.Repositories;

public class ProductRepository : GenericRepository<Product>, IProductRepository
{
    public ProductRepository(ToyStoreContext context) : base(context)
    {
    }

    // Ghi đè phương thức AddAsync nếu bạn muốn mọi lệnh thêm sản phẩm đều qua Procedure
    // Hoặc tạo một phương thức riêng như dưới đây:
    public async Task AddProductViaProcedureAsync(Product product)
    {
        var parameters = new[]
        {
            new OracleParameter("p_CategoryID", OracleDbType.Int32) { Value = product.CategoryId },
            new OracleParameter("p_ProductName", OracleDbType.NVarchar2) { Value = product.ProductName },
            new OracleParameter("p_Description", OracleDbType.NVarchar2) { Value = (object)product.Description ?? DBNull.Value },
            new OracleParameter("p_Price", OracleDbType.Decimal) { Value = product.Price },
            new OracleParameter("p_Stock", OracleDbType.Int32) { Value = product.Stock },
            new OracleParameter("p_ImageURL", OracleDbType.NVarchar2) { Value = (object)product.ImageUrl ?? DBNull.Value },
            new OracleParameter("p_Status", OracleDbType.Int32) { Value = product.Status == true ? 1 : 0 }
        };

        // Lưu ý: Oracle yêu cầu khối BEGIN ... END; khi gọi Procedure qua ExecuteSqlRaw
        string sql = "BEGIN SP_AddProduct(:p_CategoryID, :p_ProductName, :p_Description, :p_Price, :p_Stock, :p_ImageURL, :p_Status); END;";

        await _context.Database.ExecuteSqlRawAsync(sql, parameters);
    }
    public async Task UpdateProductViaProcedureAsync(Product product)
    {
        var parameters = new[]
        {
        new OracleParameter("p_ProductId", OracleDbType.Int32) { Value = product.ProductId },
        new OracleParameter("p_CategoryId", OracleDbType.Int32) { Value = product.CategoryId },
        new OracleParameter("p_ProductName", OracleDbType.Varchar2, 200) { Value = product.ProductName },
        new OracleParameter("p_Description", OracleDbType.Varchar2, 500) { Value = (object)product.Description ?? DBNull.Value },
        new OracleParameter("p_Price", OracleDbType.Decimal) { Value = product.Price },
        new OracleParameter("p_Stock", OracleDbType.Int32) { Value = product.Stock },
        new OracleParameter("p_ImageUrl", OracleDbType.Varchar2, 255) { Value = (object)product.ImageUrl ?? DBNull.Value },
        new OracleParameter("p_Status", OracleDbType.Int32) { Value = product.Status == true ? 1 : 0 }
    };

        string sql = "BEGIN \"SP_UpdateProduct\"(:p_ProductId, :p_CategoryId, :p_ProductName, :p_Description, :p_Price, :p_Stock, :p_ImageUrl, :p_Status); END;";

        await _context.Database.ExecuteSqlRawAsync(sql, parameters);
    }
    public async Task<IEnumerable<Product>> GetProductsByCategoryAsync(int categoryId)
    {
        return await _dbSet
            .Where(p => p.CategoryId == categoryId)
            .ToListAsync();
    }

    public async Task<IEnumerable<Product>> GetActiveProductsAsync()
    {
        // Vì trong DB Status là NUMBER(1), EF Core vẫn map về bool nếu cấu hình đúng
        return await _dbSet
            .Where(p => p.Status == true)
            .ToListAsync();
    }

    public async Task<IEnumerable<Product>> FilterProductsViaProcedureAsync(string? keyword, int? categoryId, decimal? minPrice, decimal? maxPrice)
    {
        var results = new List<Product>();
        var connection = _context.Database.GetDbConnection();
        var shouldCloseConnection = connection.State != ConnectionState.Open;

        if (shouldCloseConnection)
        {
            await connection.OpenAsync();
        }

        try
        {
            if (connection.CreateCommand() is not OracleCommand command)
            {
                throw new InvalidOperationException("Database connection is not Oracle.");
            }

            await using (command)
            {
                command.BindByName = true;
                command.CommandType = CommandType.Text;
                command.CommandText = "BEGIN \"SP_FILTER_PRODUCTS\"(:p_Keyword, :p_CategoryId, :p_MinPrice, :p_MaxPrice, :p_cursor); END;";

                // Nếu keyword null/rỗng -> DBNull.Value
                command.Parameters.Add(new OracleParameter("p_Keyword", OracleDbType.Varchar2)
                {
                    Value = string.IsNullOrWhiteSpace(keyword) ? DBNull.Value : keyword.Trim()
                });

                // Nếu categoryId null hoặc = 0 -> DBNull.Value (coi như không lọc danh mục)
                command.Parameters.Add(new OracleParameter("p_CategoryId", OracleDbType.Int32)
                {
                    Value = (categoryId == null || categoryId == 0) ? DBNull.Value : categoryId.Value
                });

                // Nếu minPrice null -> DBNull.Value
                command.Parameters.Add(new OracleParameter("p_MinPrice", OracleDbType.Decimal)
                {
                    Value = minPrice.HasValue ? minPrice.Value : DBNull.Value
                });

                // Nếu maxPrice null -> DBNull.Value
                command.Parameters.Add(new OracleParameter("p_MaxPrice", OracleDbType.Decimal)
                {
                    Value = maxPrice.HasValue ? maxPrice.Value : DBNull.Value
                });

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
                    results.Add(MapProductFromReader(reader));
                }
            }
        }
        finally
        {
            if (shouldCloseConnection)
            {
                await connection.CloseAsync();
            }
        }

        return results;
    }

    public async Task<Product?> GetProductWithCategoryAsync(int productId)
    {
        return await _dbSet
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.ProductId == productId);
    }

    public async Task<bool> CheckStockAvailabilityAsync(int productId, int quantity)
    {
        var product = await GetByIdAsync(productId);
        return product != null && product.Stock >= quantity;
    }
    public async Task<int> DeleteProductViaProcedureAsync(int productId)
    {
        // Tham số Input truyền vào
        var p_ProductId = new OracleParameter("p_ProductId", OracleDbType.Int32) { Value = productId };

        // Tham số Output nhận kết quả trả về từ Oracle
        var p_ResultCode = new OracleParameter("p_ResultCode", OracleDbType.Int32)
        {
            Direction = ParameterDirection.Output
        };

        string sql = "BEGIN \"SP_DeleteProduct\"(:p_ProductId, :p_ResultCode); END;";

        // Thực thi Procedure
        await _context.Database.ExecuteSqlRawAsync(sql, p_ProductId, p_ResultCode);

        // Trả giá trị Output về cho Controller xử lý hiển thị thông báo
        return Convert.ToInt32(p_ResultCode.Value.ToString());
    }

    private static Product MapProductFromReader(DbDataReader reader)
    {
        return new Product
        {
            ProductId = GetInt32(reader, "ProductID"),
            CategoryId = GetInt32(reader, "CategoryID"),
            ProductName = GetString(reader, "ProductName"),
            Description = GetNullableString(reader, "Description"),
            Price = GetDecimal(reader, "Price"),
            Stock = GetInt32(reader, "Stock"),
            ImageUrl = GetNullableString(reader, "ImageURL"),
            Status = GetNullableBool(reader, "Status")
        };
    }

    private static int GetColumnOrdinal(DbDataReader reader, string columnName)
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

    private static int GetInt32(DbDataReader reader, string columnName)
    {
        var ordinal = GetColumnOrdinal(reader, columnName);
        return reader.IsDBNull(ordinal) ? 0 : Convert.ToInt32(reader.GetValue(ordinal));
    }

    private static decimal GetDecimal(DbDataReader reader, string columnName)
    {
        var ordinal = GetColumnOrdinal(reader, columnName);
        return reader.IsDBNull(ordinal) ? 0m : Convert.ToDecimal(reader.GetValue(ordinal));
    }

    private static string GetString(DbDataReader reader, string columnName)
    {
        var ordinal = GetColumnOrdinal(reader, columnName);
        return reader.IsDBNull(ordinal) ? string.Empty : reader.GetString(ordinal);
    }

    private static string? GetNullableString(DbDataReader reader, string columnName)
    {
        var ordinal = GetColumnOrdinal(reader, columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    private static bool? GetNullableBool(DbDataReader reader, string columnName)
    {
        var ordinal = GetColumnOrdinal(reader, columnName);
        if (reader.IsDBNull(ordinal))
        {
            return null;
        }

        return Convert.ToInt32(reader.GetValue(ordinal)) != 0;
    }
}