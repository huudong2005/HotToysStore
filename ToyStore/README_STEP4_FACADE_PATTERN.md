# Bước 4: Facade Pattern cho Quy trình Thanh toán (Checkout) - Đã Hoàn Thành ✅

## Tổng quan

Dự án đã được triển khai đầy đủ **Facade Pattern** để đơn giản hóa quy trình thanh toán phức tạp, gộp nhiều bước thành một interface đơn giản.

## Cấu trúc đã triển khai

### 1. ICheckoutFacade Interface (Domain/Interfaces)

Interface định nghĩa Facade cho quy trình thanh toán:

```csharp
public interface ICheckoutFacade
{
    Task<Order> PlaceOrderAsync(
        ShoppingCart cart, 
        int customerId, 
        string? paymentMethod = null
    );
}
```

### 2. CheckoutFacade Class (Infrastructure/Facades)

Triển khai đầy đủ quy trình thanh toán với 5 bước:

#### Quy trình PlaceOrderAsync:

1. **Kiểm tra giỏ hàng**: Xác minh giỏ hàng không trống
2. **Kiểm tra tồn kho**: Validate stock availability cho tất cả sản phẩm
3. **Tính giá**: Sử dụng Strategy Pattern (DiscountService)
4. **Tạo đơn hàng & Cập nhật Stock**: Sử dụng Unit of Work với Transaction
5. **Xóa Session giỏ hàng**: Được thực hiện ở Controller sau khi thành công

### 3. Các phương thức private trong CheckoutFacade

#### ValidateStockAvailabilityAsync()
- Kiểm tra từng sản phẩm trong giỏ hàng
- Xác minh sản phẩm tồn tại
- Xác minh sản phẩm đang được bán (Status = true)
- Xác minh số lượng tồn kho đủ

#### CalculatePricing()
- Tính Subtotal = ∑(Quantity × UnitPrice)
- Sử dụng DiscountService để tính DiscountValue và FinalTotal
- Trả về tuple (Subtotal, DiscountValue, FinalTotal)

#### CreateOrderWithTransactionAsync()
- Bắt đầu transaction
- Tạo Order với thông tin đầy đủ
- Tạo OrderDetails cho từng item
- Cập nhật Stock cho từng sản phẩm
- Commit transaction hoặc Rollback nếu có lỗi

## Sử dụng trong OrderController

### Trước khi refactor (Fat Controller):

```csharp
[HttpPost]
public async Task<IActionResult> Create(Order order)
{
    // 100+ dòng code xử lý:
    // - Kiểm tra giỏ hàng
    // - Kiểm tra tồn kho
    // - Tính giá
    // - Tạo đơn hàng
    // - Tạo order details
    // - Cập nhật stock
    // - Transaction management
    // - Xóa session
}
```

### Sau khi refactor (Sử dụng Facade):

```csharp
[HttpPost]
public async Task<IActionResult> Create(Order order)
{
    try
    {
        var cart = GetCart();
        
        if (!cart.Items.Any())
        {
            TempData["ErrorMessage"] = "Giỏ hàng trống";
            return RedirectToAction("Index", "Cart");
        }

        var customerId = GetCurrentCustomerId();
        if (customerId == 0)
        {
            TempData["ErrorMessage"] = "Vui lòng đăng nhập để đặt hàng";
            return RedirectToAction("Login", "Auth");
        }

        // Sử dụng CheckoutFacade - chỉ một dòng gọi method
        var newOrder = await _checkoutFacade.PlaceOrderAsync(
            cart, 
            customerId, 
            order.PaymentMethod ?? "COD"
        );

        // Xóa Session giỏ hàng sau khi thành công
        cart.Clear();
        SaveCart(cart);

        TempData["SuccessMessage"] = $"Đặt hàng thành công! Mã đơn hàng: #{newOrder.OrderId}";
        return RedirectToAction("Details", new { id = newOrder.OrderId });
    }
    catch (InvalidOperationException ex)
    {
        TempData["ErrorMessage"] = ex.Message;
        return RedirectToAction("Checkout", "Cart");
    }
    catch (Exception ex)
    {
        TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
        return RedirectToAction("Checkout", "Cart");
    }
}
```

## Quy trình chi tiết

### Flow Diagram

```
Controller.Create()
    ↓
CheckoutFacade.PlaceOrderAsync()
    ↓
┌─────────────────────────────────────┐
│ 1. ValidateStockAvailabilityAsync() │
│    - Kiểm tra từng sản phẩm         │
│    - Xác minh tồn kho đủ            │
└─────────────────────────────────────┘
    ↓
┌─────────────────────────────────────┐
│ 2. CalculatePricing()               │
│    - Tính Subtotal                  │
│    - Sử dụng DiscountService        │
│    - Tính DiscountValue & Total     │
└─────────────────────────────────────┘
    ↓
┌─────────────────────────────────────┐
│ 3. CreateOrderWithTransactionAsync()│
│    - BeginTransaction               │
│    - Tạo Order                      │
│    - Tạo OrderDetails               │
│    - Cập nhật Stock                 │
│    - CommitTransaction              │
└─────────────────────────────────────┘
    ↓
Controller: Xóa Session giỏ hàng
    ↓
Redirect to Order Details
```

## Dependency Injection

Đã đăng ký trong `Program.cs`:

```csharp
// Đăng ký Facades
builder.Services.AddScoped<ICheckoutFacade, CheckoutFacade>();
```

## Lợi ích đã đạt được

1. **Đơn giản hóa Controller**: Controller chỉ cần gọi một method thay vì xử lý nhiều bước
2. **Tách biệt concerns**: Business logic tách khỏi Controller
3. **Dễ test**: Có thể test CheckoutFacade độc lập
4. **Dễ bảo trì**: Logic tập trung ở một nơi
5. **Tái sử dụng**: Có thể sử dụng CheckoutFacade ở nhiều nơi
6. **Transaction management**: Đảm bảo tính nhất quán dữ liệu

## Cấu trúc thư mục

```
ToyStore/
├── Domain/
│   └── Interfaces/
│       └── ICheckoutFacade.cs          # Interface định nghĩa Facade
├── Infrastructure/
│   └── Facades/
│       └── CheckoutFacade.cs          # Triển khai Facade
└── Controllers/
    └── OrderController.cs              # Sử dụng CheckoutFacade
```

## Tích hợp với các Pattern khác

### 1. Strategy Pattern (DiscountService)
- CheckoutFacade sử dụng DiscountService để tính giá
- Có thể thay đổi strategy mà không ảnh hưởng đến Facade

### 2. Unit of Work Pattern
- CheckoutFacade sử dụng IUnitOfWork để quản lý transaction
- Đảm bảo tính nhất quán dữ liệu

### 3. Repository Pattern
- CheckoutFacade sử dụng repositories thông qua UnitOfWork
- Tách biệt data access logic

## Ví dụ sử dụng

### Trong Controller

```csharp
public class OrderController : Controller
{
    private readonly ICheckoutFacade _checkoutFacade;
    
    public OrderController(ICheckoutFacade checkoutFacade)
    {
        _checkoutFacade = checkoutFacade;
    }
    
    [HttpPost]
    public async Task<IActionResult> Create(Order order)
    {
        var cart = GetCart();
        var customerId = GetCurrentCustomerId();
        
        // Chỉ cần gọi một method
        var newOrder = await _checkoutFacade.PlaceOrderAsync(
            cart, 
            customerId, 
            order.PaymentMethod
        );
        
        // Xử lý kết quả
        cart.Clear();
        SaveCart(cart);
        
        return RedirectToAction("Details", new { id = newOrder.OrderId });
    }
}
```

### Error Handling

```csharp
try
{
    var order = await _checkoutFacade.PlaceOrderAsync(cart, customerId);
    // Success
}
catch (InvalidOperationException ex)
{
    // Lỗi business logic (thiếu tồn kho, giỏ hàng trống, etc.)
    TempData["ErrorMessage"] = ex.Message;
}
catch (Exception ex)
{
    // Lỗi hệ thống
    TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
}
```

## Mở rộng trong tương lai

Có thể dễ dàng mở rộng CheckoutFacade để thêm các bước mới:

```csharp
public async Task<Order> PlaceOrderAsync(...)
{
    // Bước hiện tại
    await ValidateStockAvailabilityAsync(cart);
    var pricing = CalculatePricing(cart);
    var order = await CreateOrderWithTransactionAsync(...);
    
    // Có thể thêm:
    // - Gửi email xác nhận
    // - Tạo invoice
    // - Cập nhật loyalty points
    // - Gửi notification
    
    return order;
}
```

## Kết luận

✅ **Bước 4 đã hoàn thành đầy đủ:**
- ICheckoutFacade interface đã được tạo với PlaceOrderAsync()
- CheckoutFacade class đã triển khai đầy đủ 5 bước:
  1. Kiểm tra tồn kho
  2. Tính giá (Strategy Pattern)
  3. Tạo đơn hàng (Unit of Work)
  4. Cập nhật Stock
  5. Xóa Session giỏ hàng (ở Controller)
- OrderController đã được refactor để sử dụng CheckoutFacade
- CheckoutFacade đã được đăng ký trong DI container
- Controller chỉ cần gọi `_checkoutFacade.PlaceOrderAsync()`

**Facade Pattern đã được triển khai thành công và sẵn sàng sử dụng!**
