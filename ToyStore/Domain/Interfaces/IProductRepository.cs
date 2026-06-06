using ToyStore.Domain.Entities;

namespace ToyStore.Domain.Interfaces;

public interface IProductRepository : IGenericRepository<Product>
{
    // Thêm phương thức để gọi Procedure từ Oracle
    Task AddProductViaProcedureAsync(Product product);

    Task<IEnumerable<Product>> GetProductsByCategoryAsync(int categoryId);
    Task<IEnumerable<Product>> GetActiveProductsAsync();
    Task<IEnumerable<Product>> FilterProductsViaProcedureAsync(string? keyword, int? categoryId, decimal? minPrice, decimal? maxPrice);
    Task<Product?> GetProductWithCategoryAsync(int productId);
    Task<bool> CheckStockAvailabilityAsync(int productId, int quantity);
    Task UpdateProductViaProcedureAsync(Product product);
    Task<int> DeleteProductViaProcedureAsync(int productId);
}