using ToyStore.Domain.Entities;

namespace ToyStore.Domain.Interfaces;

public interface ICategoryRepository : IGenericRepository<Category>
{
    Task<Category?> GetCategoryWithProductsAsync(int categoryId);
    Task<IEnumerable<Category>> GetCategoriesWithActiveProductsAsync();

    // 3 hàm Stored Procedure mới cho Oracle
    Task AddCategoryViaProcedureAsync(Category category);
    Task UpdateCategoryViaProcedureAsync(Category category);
    Task<int> DeleteCategoryViaProcedureAsync(int categoryId);
}
