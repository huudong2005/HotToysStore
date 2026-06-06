# Bước 1: Repository & Unit of Work Pattern - Đã Hoàn Thành ✅

## Tổng quan

Dự án đã được triển khai đầy đủ **Repository Pattern** và **Unit of Work Pattern** theo Clean Architecture.

## Cấu trúc đã triển khai

### 1. Generic Repository Pattern

#### `IGenericRepository<T>` (Domain/Interfaces)
- Interface cốt lõi cho các thao tác CRUD cơ bản
- Các phương thức:
  - `GetByIdAsync(int id)`: Lấy entity theo ID
  - `GetAllAsync()`: Lấy tất cả entities
  - `FindAsync(Expression<Func<T, bool>>)`: Tìm kiếm với điều kiện
  - `FirstOrDefaultAsync(Expression<Func<T, bool>>)`: Lấy entity đầu tiên thỏa điều kiện
  - `AnyAsync(Expression<Func<T, bool>>)`: Kiểm tra tồn tại
  - `AddAsync(T entity)`: Thêm entity
  - `AddRangeAsync(IEnumerable<T>)`: Thêm nhiều entities
  - `Update(T entity)`: Cập nhật entity
  - `UpdateRange(IEnumerable<T>)`: Cập nhật nhiều entities
  - `Remove(T entity)`: Xóa entity
  - `RemoveRange(IEnumerable<T>)`: Xóa nhiều entities
  - `Query()`: Trả về IQueryable để truy vấn phức tạp

#### `GenericRepository<T>` (Infrastructure/Repositories)
- Triển khai đầy đủ `IGenericRepository<T>`
- Sử dụng Entity Framework Core
- Protected `_dbSet` để các repository con có thể truy cập

### 2. Specific Repositories

Các repository cụ thể kế thừa từ `GenericRepository<T>` và triển khai interface riêng:

#### `IProductRepository` & `ProductRepository`
- `GetProductsByCategoryAsync(int categoryId)`: Lấy sản phẩm theo danh mục
- `GetActiveProductsAsync()`: Lấy sản phẩm đang bán
- `SearchProductsByNameAsync(string searchName)`: Tìm kiếm sản phẩm theo tên
- `GetProductWithCategoryAsync(int productId)`: Lấy sản phẩm kèm danh mục
- `CheckStockAvailabilityAsync(int productId, int quantity)`: Kiểm tra tồn kho

#### `ICategoryRepository` & `CategoryRepository`
- `GetCategoryWithProductsAsync(int categoryId)`: Lấy danh mục kèm sản phẩm
- `GetCategoriesWithActiveProductsAsync()`: Lấy danh mục có sản phẩm đang bán

#### `ICustomerRepository` & `CustomerRepository`
- `GetCustomerByEmailAsync(string email)`: Lấy khách hàng theo email
- `EmailExistsAsync(string email)`: Kiểm tra email đã tồn tại

#### `IOrderRepository` & `OrderRepository`
- `GetOrdersByCustomerIdAsync(int customerId)`: Lấy đơn hàng theo khách hàng
- `GetOrderWithDetailsAsync(int orderId)`: Lấy đơn hàng kèm chi tiết
- `GetOrdersByStatusAsync(string status)`: Lấy đơn hàng theo trạng thái

#### `IOrderDetailRepository` & `OrderDetailRepository`
- `GetOrderDetailsByOrderIdAsync(int orderId)`: Lấy chi tiết đơn hàng

#### `IAdminRepository` & `AdminRepository`
- Quản lý Admin/Staff

### 3. Unit of Work Pattern

#### `IUnitOfWork` (Domain/Interfaces)
- Quản lý tất cả repositories:
  - `IProductRepository Products { get; }`
  - `ICategoryRepository Categories { get; }`
  - `ICustomerRepository Customers { get; }`
  - `IOrderRepository Orders { get; }`
  - `IOrderDetailRepository OrderDetails { get; }`
  - `IAdminRepository Admins { get; }`
  - `IGenericRepository<Cart> Carts { get; }`
  - `IGenericRepository<CartItem> CartItems { get; }`

- **Transaction Management** (Quan trọng cho yêu cầu):
  - `Task BeginTransactionAsync()`: Bắt đầu transaction
  - `Task CommitTransactionAsync()`: Commit transaction
  - `Task RollbackTransactionAsync()`: Rollback transaction
  - `Task<int> SaveChangesAsync()`: Lưu thay đổi

#### `UnitOfWork` (Infrastructure/UnitOfWork)
- Triển khai đầy đủ `IUnitOfWork`
- Lazy initialization cho các repositories
- Quản lý transaction với `IDbContextTransaction`
- Dispose pattern để giải phóng tài nguyên

### 4. Dependency Injection

Đã đăng ký trong `Program.cs`:
```csharp
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
```

## Sử dụng trong OrderController (Ví dụ quan trọng)

### Đặt hàng với Transaction (Create Order)

```csharp
// Begin transaction để đảm bảo atomicity
await _unitOfWork.BeginTransactionAsync();

try
{
    // 1. Tạo đơn hàng
    var newOrder = new Order { ... };
    await _unitOfWork.Orders.AddAsync(newOrder);
    await _unitOfWork.SaveChangesAsync();

    // 2. Tạo chi tiết đơn hàng và cập nhật tồn kho
    foreach (var item in cart.Items)
    {
        // Kiểm tra tồn kho
        var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId);
        if (product.Stock < item.Quantity)
        {
            throw new Exception("Không đủ tồn kho");
        }

        // Tạo order detail
        var orderDetail = new OrderDetail { ... };
        await _unitOfWork.OrderDetails.AddAsync(orderDetail);

        // Trừ tồn kho
        product.Stock -= item.Quantity;
        _unitOfWork.Products.Update(product);
    }

    // 3. Lưu tất cả thay đổi
    await _unitOfWork.SaveChangesAsync();

    // 4. Commit transaction - TẤT CẢ hoặc KHÔNG CÓ GÌ
    await _unitOfWork.CommitTransactionAsync();
}
catch
{
    // Rollback nếu có lỗi - đảm bảo tính nhất quán
    await _unitOfWork.RollbackTransactionAsync();
    throw;
}
```

### Hủy đơn hàng với Transaction (Cancel Order)

```csharp
await _unitOfWork.BeginTransactionAsync();

try
{
    // Hoàn trả tồn kho
    foreach (var detail in order.OrderDetails)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(detail.ProductId);
        product.Stock += detail.Quantity;
        _unitOfWork.Products.Update(product);
    }

    // Cập nhật trạng thái đơn hàng
    order.Status = "Cancelled";
    _unitOfWork.Orders.Update(order);
    
    await _unitOfWork.SaveChangesAsync();
    await _unitOfWork.CommitTransactionAsync();
}
catch
{
    await _unitOfWork.RollbackTransactionAsync();
    throw;
}
```

## Đảm bảo tính nhất quán dữ liệu

✅ **Đặt hàng**: Khi tạo đơn hàng và trừ tồn kho phải diễn ra **đồng thời** hoặc **hủy bỏ toàn bộ**
- Sử dụng transaction để đảm bảo atomicity
- Nếu có lỗi ở bất kỳ bước nào, toàn bộ transaction sẽ rollback
- Tồn kho chỉ bị trừ khi đơn hàng được tạo thành công

✅ **Hủy đơn hàng**: Khi hủy đơn hàng, tồn kho phải được hoàn trả
- Sử dụng transaction để đảm bảo tồn kho được hoàn trả cùng lúc với việc cập nhật trạng thái đơn hàng

## Cấu trúc thư mục

```
ToyStore/
├── Domain/
│   ├── Entities/          # Các entity classes
│   └── Interfaces/        # IGenericRepository, IUnitOfWork, I*Repository
├── Infrastructure/
│   ├── Data/
│   │   └── ToyStoreContext.cs
│   ├── Repositories/
│   │   ├── GenericRepository.cs
│   │   ├── ProductRepository.cs
│   │   ├── CategoryRepository.cs
│   │   ├── CustomerRepository.cs
│   │   ├── OrderRepository.cs
│   │   ├── OrderDetailRepository.cs
│   │   └── AdminRepository.cs
│   └── UnitOfWork/
│       └── UnitOfWork.cs
└── Controllers/
    └── OrderController.cs  # Sử dụng UnitOfWork với transaction
```

## Lợi ích đã đạt được

1. **Tách biệt concerns**: Data access logic tách khỏi business logic
2. **Dễ test**: Có thể mock repositories và unit of work
3. **Tái sử dụng**: Generic repository cho các entity tương tự
4. **Transaction management**: Đảm bảo tính nhất quán dữ liệu
5. **Maintainability**: Dễ bảo trì và mở rộng

## Kết luận

✅ **Bước 1 đã hoàn thành đầy đủ:**
- Generic Repository Pattern đã được triển khai
- Unit of Work Pattern đã được triển khai với transaction management
- OrderController đã sử dụng UnitOfWork với transaction để đảm bảo tính nhất quán dữ liệu
- Tất cả repositories đã được đăng ký trong UnitOfWork
- Dependency Injection đã được cấu hình

**Sẵn sàng cho Bước 2: State Pattern cho Order Lifecycle**
