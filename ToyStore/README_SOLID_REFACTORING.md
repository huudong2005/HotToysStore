# Refactoring theo SOLID Principles và Dependency Injection

## Tổng quan

Dự án đã được refactor để tuân thủ các nguyên lý SOLID và sử dụng Dependency Injection (DI) một cách chuyên nghiệp. Tất cả các interface đã được đăng ký trong `Program.cs` và Session-based Authentication đã được tách ra thành một service riêng.

## Các thay đổi chính

### 1. Tạo ISessionService và SessionService

**Mục đích**: Tách logic xử lý Session-based Authentication ra khỏi Controllers và Helpers, tuân thủ **Single Responsibility Principle (SRP)**.

**Files**:
- `Domain/Interfaces/ISessionService.cs`: Interface định nghĩa các phương thức xử lý session
- `Services/SessionService.cs`: Implementation của ISessionService

**Các phương thức**:
- `SetUserSession()`: Lưu thông tin user vào session
- `GetUserSession()`: Lấy thông tin user từ session
- `ClearSession()`: Xóa toàn bộ session (đăng xuất)
- `IsAuthenticated()`: Kiểm tra user đã đăng nhập
- `IsAdmin()`, `IsStaff()`, `IsCustomer()`: Kiểm tra loại user
- `HasRole()`, `HasAnyRole()`: Kiểm tra quyền
- `GetUserId()`: Lấy UserId từ session

### 2. Refactor Controllers

**Các Controllers đã được cập nhật**:
- `AuthController`: Sử dụng `ISessionService` thay vì truy cập session trực tiếp
- `OrderController`: Sử dụng `ISessionService.GetUserId()` thay vì `GetCurrentCustomerId()`
- `CartController`: Sử dụng `ISessionService.GetUserId()` thay vì `GetCurrentCustomerId()`
- `HomeController`: Sử dụng `ISessionService.GetUserSession()` thay vì `AuthHelper.GetCurrentUser()`

**Lợi ích**:
- Controllers không còn phụ thuộc trực tiếp vào HttpContext.Session
- Dễ dàng test và mock
- Code sạch hơn, tuân thủ **Dependency Inversion Principle (DIP)**

### 3. Refactor SessionMiddleware

**File**: `Middleware/SessionMiddleware.cs`

**Thay đổi**: Middleware bây giờ sử dụng `ISessionService` thay vì truy cập session trực tiếp.

**Lợi ích**:
- Middleware tuân thủ DI pattern
- Dễ dàng test và maintain

### 4. Đăng ký tất cả Interfaces trong Program.cs

**File**: `Program.cs`

**Các interfaces đã được đăng ký**:

```csharp
// Unit of Work
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<DiscountService>();
builder.Services.AddScoped<DataInitializationService>();

// Factories
builder.Services.AddScoped<IUserFactory, UserFactory>();

// Facades
builder.Services.AddScoped<ICheckoutFacade, CheckoutFacade>();
```

**Lưu ý**: Các repository interfaces (IProductRepository, ICategoryRepository, etc.) không cần đăng ký riêng vì chúng được truy cập thông qua `IUnitOfWork`.

### 5. Giữ nguyên AuthHelper (Backward Compatibility)

**File**: `Helpers/AuthHelper.cs`

**Lý do**: Giữ lại `AuthHelper` để tương thích ngược với các code cũ (ví dụ: `AuthorizeRoleAttribute`).

**Khuyến nghị**: Nên sử dụng `ISessionService` trong code mới thay vì `AuthHelper`.

## SOLID Principles được áp dụng

### 1. Single Responsibility Principle (SRP)
- `SessionService`: Chỉ chịu trách nhiệm xử lý session
- `AuthService`: Chỉ chịu trách nhiệm xác thực và đăng ký
- Mỗi service có một trách nhiệm rõ ràng

### 2. Open/Closed Principle (OCP)
- Có thể mở rộng `ISessionService` với các phương thức mới mà không cần sửa code cũ
- Có thể thêm các strategy mới cho discount mà không sửa code hiện tại

### 3. Liskov Substitution Principle (LSP)
- Tất cả các implementation của interfaces có thể thay thế cho nhau
- `SessionService` có thể thay thế cho bất kỳ implementation nào của `ISessionService`

### 4. Interface Segregation Principle (ISP)
- `ISessionService` chỉ chứa các phương thức cần thiết cho session management
- Không có interface "fat" với nhiều phương thức không liên quan

### 5. Dependency Inversion Principle (DIP)
- Controllers phụ thuộc vào `ISessionService` (abstraction) chứ không phụ thuộc vào `SessionService` (concrete class)
- Tất cả dependencies được inject qua constructor

## Dependency Injection Pattern

### Constructor Injection
Tất cả dependencies được inject qua constructor:

```csharp
public class OrderController : Controller
{
    private readonly ISessionService _sessionService;
    
    public OrderController(ISessionService sessionService)
    {
        _sessionService = sessionService;
    }
}
```

### Service Lifetime
- **Scoped**: `IUnitOfWork`, `ISessionService`, `IAuthService`, `ICheckoutFacade`
  - Một instance cho mỗi HTTP request
  - Phù hợp cho các service có state (như UnitOfWork với transaction)

## Cách sử dụng ISessionService

### Trong Controllers

```csharp
public class MyController : Controller
{
    private readonly ISessionService _sessionService;
    
    public MyController(ISessionService sessionService)
    {
        _sessionService = sessionService;
    }
    
    public IActionResult MyAction()
    {
        // Kiểm tra đăng nhập
        if (!_sessionService.IsAuthenticated(HttpContext))
        {
            return RedirectToAction("Login", "Auth");
        }
        
        // Lấy UserId
        var userId = _sessionService.GetUserId(HttpContext);
        
        // Lấy thông tin user
        var user = _sessionService.GetUserSession(HttpContext);
        
        // Kiểm tra role
        if (_sessionService.IsAdmin(HttpContext))
        {
            // Admin logic
        }
        
        return View();
    }
}
```

## Testing

Với DI pattern, việc test trở nên dễ dàng hơn:

```csharp
// Mock ISessionService trong unit test
var mockSessionService = new Mock<ISessionService>();
mockSessionService.Setup(s => s.IsAuthenticated(It.IsAny<HttpContext>()))
    .Returns(true);
mockSessionService.Setup(s => s.GetUserId(It.IsAny<HttpContext>()))
    .Returns(1);

var controller = new OrderController(mockSessionService.Object, ...);
```

## Migration Guide

### Thay thế AuthHelper bằng ISessionService

**Trước**:
```csharp
var user = AuthHelper.GetCurrentUser(HttpContext);
var isAdmin = AuthHelper.IsAdmin(HttpContext);
```

**Sau**:
```csharp
var user = _sessionService.GetUserSession(HttpContext);
var isAdmin = _sessionService.IsAdmin(HttpContext);
```

### Thay thế truy cập session trực tiếp

**Trước**:
```csharp
var userId = HttpContext.Session.GetString("UserId");
HttpContext.Session.SetString("UserId", userId.ToString());
```

**Sau**:
```csharp
var userId = _sessionService.GetUserId(HttpContext);
_sessionService.SetUserSession(HttpContext, userSession);
```

## Kết luận

Dự án đã được refactor thành công để:
- ✅ Tuân thủ SOLID principles
- ✅ Sử dụng Dependency Injection đầy đủ
- ✅ Tách biệt Session-based Authentication vào service riêng
- ✅ Code sạch, dễ maintain và test
- ✅ Tất cả interfaces được đăng ký trong Program.cs

**Build Status**: ✅ Build succeeded với 0 errors
