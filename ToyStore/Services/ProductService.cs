using ToyStore.Domain.Entities;
using ToyStore.Domain.Interfaces;
using ToyStore.Application.Interfaces;

namespace ToyStore.Infrastructure.Services; // Hoặc namespace theo project của bạn

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;

    public ProductService(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task CreateProductAsync(Product product)
    {
        // Bạn có thể thêm logic kiểm tra nghiệp vụ ở đây trước khi lưu
        if (product.Price < 0) throw new Exception("Giá sản phẩm không thể âm");

        // Gọi phương thức sử dụng Procedure mà chúng ta vừa tạo
        await _productRepository.AddProductViaProcedureAsync(product);
    }
}