using ToyStore.Domain.Entities;

namespace ToyStore.Application.Interfaces;

public interface IProductService
{
    Task CreateProductAsync(Product product);
    // Các phương thức khác như GetProducts, Update, Delete...
}