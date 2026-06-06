using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using ToyStore.Domain.Entities;
using ToyStore.Domain.Interfaces;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Infrastructure.Repositories;

public class CategoryRepository : GenericRepository<Category>, ICategoryRepository
{
    public CategoryRepository(ToyStoreContext context) : base(context)
    {
    }

    // Giữ nguyên 2 hàm nghiệp vụ cũ của bạn
    public async Task<Category?> GetCategoryWithProductsAsync(int categoryId)
    {
        return await _dbSet
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.CategoryId == categoryId);
    }

    public async Task<IEnumerable<Category>> GetCategoriesWithActiveProductsAsync()
    {
        // Chú ý: Đổi p.Status == true thành p.Status do cấu hình bool? map sang int của Oracle
        return await _dbSet
            .Include(c => c.Products.Where(p => p.Status == true))
            .Where(c => c.Products.Any(p => p.Status == true))
            .ToListAsync();
    }
    // ---- ĐOẠN CODE THÊM MỚI GỌI STORED PROCEDURE ----
    public async Task AddCategoryViaProcedureAsync(Category category)
    {
        var parameters = new[]
        {
            new OracleParameter("p_CategoryName", OracleDbType.Varchar2, 100) { Value = category.CategoryName }
        };

        string sql = "BEGIN \"SP_AddCategory\"(:p_CategoryName); END;";
        await _context.Database.ExecuteSqlRawAsync(sql, parameters);
    }

    public async Task UpdateCategoryViaProcedureAsync(Category category)
    {
        var parameters = new[]
        {
            new OracleParameter("p_CategoryId", OracleDbType.Int32) { Value = category.CategoryId },
            new OracleParameter("p_CategoryName", OracleDbType.Varchar2, 100) { Value = category.CategoryName }
        };

        string sql = "BEGIN \"SP_UpdateCategory\"(:p_CategoryId, :p_CategoryName); END;";
        await _context.Database.ExecuteSqlRawAsync(sql, parameters);
    }

    public async Task<int> DeleteCategoryViaProcedureAsync(int categoryId)
    {
        var p_CategoryId = new OracleParameter("p_CategoryId", OracleDbType.Int32) { Value = categoryId };
        var p_ResultCode = new OracleParameter("p_ResultCode", OracleDbType.Int32) { Direction = ParameterDirection.Output };

        string sql = "BEGIN \"SP_DeleteCategory\"(:p_CategoryId, :p_ResultCode); END;";
        await _context.Database.ExecuteSqlRawAsync(sql, p_CategoryId, p_ResultCode);

        return Convert.ToInt32(p_ResultCode.Value.ToString());
    }
}
