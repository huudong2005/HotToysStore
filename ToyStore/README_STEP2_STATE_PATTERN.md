# Bước 2: State Pattern cho Order Lifecycle - Đã Hoàn Thành ✅

## Tổng quan

Dự án đã được triển khai đầy đủ **State Pattern** để quản lý vòng đời đơn hàng (Order Lifecycle), đảm bảo logic chuyển đổi trạng thái đơn hàng được kiểm soát chặt chẽ và an toàn.

## Cấu trúc đã triển khai

### 1. IOrderState Interface (Domain/Interfaces)

Interface định nghĩa các hành vi của Order State:

```csharp
public interface IOrderState
{
    string StateName { get; }
    IOrderState Confirm(Order order);
    IOrderState Ship(Order order);
    IOrderState Cancel(Order order);
}
```

### 2. Các State Classes (Domain/States)

#### PendingState (Đơn hàng đang chờ xử lý)
- **Có thể**: `Confirm()` → Confirmed, `Cancel()` → Cancelled
- **Không thể**: `Ship()` → Throw InvalidOperationException

#### ConfirmedState (Đơn hàng đã được xác nhận)
- **Có thể**: `Ship()` → Shipped, `Cancel()` → Cancelled
- **Không thể**: `Confirm()` → Throw InvalidOperationException

#### ShippedState (Đơn hàng đã được giao)
- **Không thể**: `Confirm()`, `Ship()`, `Cancel()` → Tất cả đều throw InvalidOperationException
- **Logic**: Đơn hàng đã giao không thể hủy (theo yêu cầu)

#### CancelledState (Đơn hàng đã bị hủy)
- **Không thể**: `Confirm()`, `Ship()`, `Cancel()` → Tất cả đều throw InvalidOperationException
- **Logic**: Đơn hàng đã hủy không thể thay đổi trạng thái

### 3. OrderStateFactory (Domain/States)

Factory class để tạo IOrderState từ Status string:

```csharp
public static class OrderStateFactory
{
    public static IOrderState CreateState(string? status);
    public static IOrderState GetState(Order order);
}
```

### 4. OrderExtensions (Domain/Entities)

Extension methods cho Order entity để sử dụng State Pattern dễ dàng:

```csharp
public static class OrderExtensions
{
    // Lấy current state
    public static IOrderState GetState(this Order order);
    
    // Thực hiện các hành động
    public static void Confirm(this Order order);
    public static void Ship(this Order order);
    public static void Cancel(this Order order);
    
    // Kiểm tra khả năng thực hiện
    public static bool CanConfirm(this Order order);
    public static bool CanShip(this Order order);
    public static bool CanCancel(this Order order);
}
```

## Logic chuyển đổi trạng thái

### State Transition Diagram

```
Pending ──[Confirm]──> Confirmed ──[Ship]──> Shipped
   │                      │
   └──[Cancel]────────────┴──[Cancel]──> Cancelled
```

### Quy tắc chuyển đổi

1. **Pending → Confirmed**: Chỉ có thể xác nhận đơn hàng ở trạng thái Pending
2. **Confirmed → Shipped**: Chỉ có thể giao hàng khi đã xác nhận
3. **Pending/Confirmed → Cancelled**: Có thể hủy đơn hàng ở cả hai trạng thái
4. **Shipped → Không thể thay đổi**: Đơn hàng đã giao không thể hủy hoặc thay đổi

## Sử dụng trong Controllers

### OrderController (Customer)

#### Hủy đơn hàng với State Pattern

```csharp
[HttpPost]
public async Task<IActionResult> Cancel(int id)
{
    var order = await _unitOfWork.Orders.GetOrderWithDetailsAsync(id);
    
    // Kiểm tra bằng State Pattern
    if (!order.CanCancel())
    {
        var stateName = order.GetState().StateName;
        TempData["ErrorMessage"] = $"Không thể hủy đơn hàng ở trạng thái {stateName}...";
        return RedirectToAction("MyOrders");
    }
    
    // Sử dụng State Pattern để hủy
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
        
        // Hủy đơn hàng bằng State Pattern
        order.Cancel();
        _unitOfWork.Orders.Update(order);
        await _unitOfWork.SaveChangesAsync();
        
        await _unitOfWork.CommitTransactionAsync();
    }
    catch
    {
        await _unitOfWork.RollbackTransactionAsync();
        throw;
    }
}
```

### OrdersController (Admin/Staff)

#### Xác nhận đơn hàng

```csharp
[HttpPost]
public async Task<IActionResult> Confirm(int id)
{
    var order = await _unitOfWork.Orders.GetByIdAsync(id);
    
    // Kiểm tra bằng State Pattern
    if (!order.CanConfirm())
    {
        var stateName = order.GetState().StateName;
        TempData["ErrorMessage"] = $"Không thể xác nhận đơn hàng ở trạng thái {stateName}...";
        return RedirectToAction("Index");
    }
    
    // Sử dụng State Pattern để xác nhận
    order.Confirm();
    _unitOfWork.Orders.Update(order);
    await _unitOfWork.SaveChangesAsync();
}
```

#### Giao hàng

```csharp
[HttpPost]
public async Task<IActionResult> Ship(int id)
{
    var order = await _unitOfWork.Orders.GetByIdAsync(id);
    
    // Kiểm tra bằng State Pattern
    if (!order.CanShip())
    {
        var stateName = order.GetState().StateName;
        TempData["ErrorMessage"] = $"Không thể giao hàng ở trạng thái {stateName}...";
        return RedirectToAction("Index");
    }
    
    // Sử dụng State Pattern để giao hàng
    order.Ship();
    _unitOfWork.Orders.Update(order);
    await _unitOfWork.SaveChangesAsync();
}
```

#### Hủy đơn hàng (Admin/Staff)

```csharp
[HttpPost]
public async Task<IActionResult> Cancel(int id)
{
    var order = await _unitOfWork.Orders.GetOrderWithDetailsAsync(id);
    
    // Kiểm tra bằng State Pattern
    if (!order.CanCancel())
    {
        var stateName = order.GetState().StateName;
        TempData["ErrorMessage"] = $"Không thể hủy đơn hàng ở trạng thái {stateName}...";
        return RedirectToAction("Index");
    }
    
    // Sử dụng State Pattern với transaction để đảm bảo hoàn trả tồn kho
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
        
        // Hủy đơn hàng bằng State Pattern
        order.Cancel();
        _unitOfWork.Orders.Update(order);
        await _unitOfWork.SaveChangesAsync();
        
        await _unitOfWork.CommitTransactionAsync();
    }
    catch
    {
        await _unitOfWork.RollbackTransactionAsync();
        throw;
    }
}
```

## Đảm bảo logic yêu cầu

### ✅ Pending có thể Cancel
- `PendingState.Cancel()` → Chuyển sang CancelledState
- Không throw exception

### ✅ Shipped không thể Cancel
- `ShippedState.Cancel()` → Throw `InvalidOperationException`
- Message: "Không thể hủy đơn hàng khi đã được giao. Đơn hàng đã ở trạng thái Shipped."

### ✅ Confirmed có thể Cancel
- `ConfirmedState.Cancel()` → Chuyển sang CancelledState
- Không throw exception

## Cấu trúc thư mục

```
ToyStore/
├── Domain/
│   ├── Entities/
│   │   ├── Order.cs
│   │   └── OrderExtensions.cs          # Extension methods cho State Pattern
│   ├── Interfaces/
│   │   └── IOrderState.cs              # Interface định nghĩa State
│   └── States/
│       ├── OrderStateFactory.cs        # Factory để tạo State
│       ├── PendingState.cs             # State: Pending
│       ├── ConfirmedState.cs           # State: Confirmed
│       ├── ShippedState.cs             # State: Shipped
│       └── CancelledState.cs           # State: Cancelled
└── Controllers/
    ├── OrderController.cs               # Customer: Sử dụng State Pattern
    └── OrdersController.cs             # Admin/Staff: Sử dụng State Pattern
```

## Lợi ích đã đạt được

1. **Kiểm soát chặt chẽ**: Logic chuyển đổi trạng thái được kiểm soát bởi State Pattern
2. **Tránh lỗi logic**: Không thể chuyển đổi trạng thái không hợp lệ (ví dụ: Shipped → Cancelled)
3. **Dễ mở rộng**: Thêm state mới chỉ cần tạo class mới implement IOrderState
4. **Code sạch**: Controllers không cần kiểm tra điều kiện phức tạp, chỉ cần gọi extension methods
5. **Dễ test**: Có thể test từng state class độc lập
6. **Maintainability**: Logic state tập trung ở một nơi, dễ bảo trì

## Ví dụ sử dụng

### Kiểm tra trước khi thực hiện hành động

```csharp
if (order.CanCancel())
{
    order.Cancel();
    // Lưu vào database
}
else
{
    // Hiển thị thông báo lỗi
}
```

### Lấy thông tin state hiện tại

```csharp
var currentState = order.GetState();
var stateName = currentState.StateName; // "Pending", "Confirmed", "Shipped", "Cancelled"
```

### Xử lý exception khi chuyển đổi không hợp lệ

```csharp
try
{
    order.Ship(); // Nếu order.Status != "Confirmed", sẽ throw InvalidOperationException
}
catch (InvalidOperationException ex)
{
    // Xử lý lỗi: ex.Message chứa thông báo chi tiết
}
```

## Kết luận

✅ **Bước 2 đã hoàn thành đầy đủ:**
- IOrderState interface đã được tạo với các phương thức Confirm(), Ship(), Cancel()
- Các state classes đã được triển khai: PendingState, ConfirmedState, ShippedState, CancelledState
- Logic đảm bảo: Pending có thể Cancel, Shipped không thể Cancel
- OrderController và OrdersController đã sử dụng State Pattern
- OrderExtensions cung cấp API dễ sử dụng
- OrderStateFactory quản lý việc tạo State từ Status string

**State Pattern đã được triển khai thành công và sẵn sàng sử dụng!**
