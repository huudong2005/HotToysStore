# Bước 5: Factory Method Pattern cho Phân quyền Người dùng - Đã Hoàn Thành ✅

## Tổng quan

Dự án đã được triển khai đầy đủ **Factory Method Pattern** để tạo các đối tượng User (Customer, Staff, Admin) với Role/Claims mặc định, đảm bảo tính nhất quán và dễ bảo trì.

## Cấu trúc đã triển khai

### 1. IUserFactory Interface (Domain/Interfaces)

Interface định nghĩa Factory Method Pattern cho việc tạo các đối tượng User:

```csharp
public interface IUserFactory
{
    Customer CreateCustomer(RegisterViewModel model, string passwordHash);
    Admin CreateStaff(CreateStaffViewModel model, string passwordHash);
    Admin CreateAdmin(CreateStaffViewModel model, string passwordHash);
    Admin CreateAdmin(string username, string passwordHash, string? fullName = null, string role = "Admin");
}
```

### 2. UserFactory Class (Domain/Factories)

Triển khai Factory Method Pattern với các factory methods:

#### CreateCustomer()
- Tạo Customer với Role mặc định: "Customer"
- Set CreatedAt = DateTime.Now
- Map tất cả thông tin từ RegisterViewModel

#### CreateStaff()
- Tạo Admin entity với Role = "Staff"
- Sử dụng Username từ CreateStaffViewModel
- Set Role mặc định: "Staff"

#### CreateAdmin() (từ CreateStaffViewModel)
- Tạo Admin entity với Role = "Admin"
- Sử dụng Username từ CreateStaffViewModel
- Set Role mặc định: "Admin"

#### CreateAdmin() (từ tham số)
- Tạo Admin entity với tham số tùy chỉnh
- Dùng cho tạo admin mặc định hoặc admin đặc biệt
- Có thể chỉ định Role (Admin hoặc Staff)

### 3. Constants cho Roles

```csharp
private const string CUSTOMER_ROLE = "Customer";
private const string STAFF_ROLE = "Staff";
private const string ADMIN_ROLE = "Admin";
```

## Sử dụng trong AuthService

### Đăng ký Customer

```csharp
private async Task<bool> RegisterCustomerAsync(RegisterViewModel model)
{
    if (await IsEmailExistsAsync(model.Email))
        return false;

    // Sử dụng UserFactory để tạo Customer với Role/Claims mặc định
    var passwordHash = HashPassword(model.Password);
    var customer = _userFactory.CreateCustomer(model, passwordHash);

    _context.Customers.Add(customer);
    await _context.SaveChangesAsync();
    return true;
}
```

### Tạo Staff/Admin

```csharp
public async Task<bool> CreateStaffAsync(CreateStaffViewModel model)
{
    if (await IsUsernameExistsAsync(model.Username))
        return false;

    // Sử dụng UserFactory để tạo Staff hoặc Admin dựa trên Role
    var passwordHash = HashPassword(model.Password);
    Admin admin;

    if (model.Role?.ToLower() == "admin")
    {
        // Tạo Admin với Role = "Admin"
        admin = _userFactory.CreateAdmin(model, passwordHash);
    }
    else
    {
        // Tạo Staff với Role = "Staff"
        admin = _userFactory.CreateStaff(model, passwordHash);
    }

    _context.Admins.Add(admin);
    await _context.SaveChangesAsync();
    return true;
}
```

## DataInitializationService

Service để khởi tạo admin mặc định khi khởi động ứng dụng:

```csharp
public class DataInitializationService
{
    public async Task InitializeDefaultAdminAsync()
    {
        var existingAdmin = await _context.Admins
            .FirstOrDefaultAsync(a => a.Username == "admin");

        if (existingAdmin == null)
        {
            // Sử dụng UserFactory để tạo admin mặc định
            var passwordHash = _authService.HashPassword("admin123");
            var defaultAdmin = _userFactory.CreateAdmin(
                username: "admin",
                passwordHash: passwordHash,
                fullName: "Administrator",
                role: "Admin"
            );

            _context.Admins.Add(defaultAdmin);
            await _context.SaveChangesAsync();
        }
    }
}
```

## Role/Claims mặc định

### Customer
- **Role**: "Customer"
- **UserType**: "Customer"
- **Claims**: Có thể mua hàng, xem sản phẩm, quản lý giỏ hàng

### Staff
- **Role**: "Staff"
- **UserType**: "Staff"
- **Claims**: Quản lý sản phẩm, danh mục, đơn hàng, khách hàng

### Admin
- **Role**: "Admin"
- **UserType**: "Admin"
- **Claims**: Tất cả quyền của Staff + Quản lý nhân viên, toàn quyền hệ thống

## Dependency Injection

Đã đăng ký trong `Program.cs`:

```csharp
// Đăng ký Factories
builder.Services.AddScoped<IUserFactory, UserFactory>();

// Đăng ký Data Initialization Service
builder.Services.AddScoped<DataInitializationService>();
```

## Khởi tạo dữ liệu mặc định

Trong `Program.cs`, admin mặc định được tạo tự động khi khởi động:

```csharp
// Khởi tạo dữ liệu mặc định (admin account)
try
{
    using var scope = app.Services.CreateScope();
    var dataInitService = scope.ServiceProvider.GetRequiredService<DataInitializationService>();
    await dataInitService.InitializeDefaultAdminAsync();
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "Error initializing default data");
}
```

**Tài khoản admin mặc định:**
- Username: `admin`
- Password: `admin123`
- Role: `Admin`
- FullName: `Administrator`

## Cấu trúc thư mục

```
ToyStore/
├── Domain/
│   ├── Interfaces/
│   │   └── IUserFactory.cs          # Interface định nghĩa Factory
│   └── Factories/
│       └── UserFactory.cs            # Triển khai Factory Method Pattern
├── Services/
│   ├── AuthService.cs                 # Sử dụng UserFactory
│   └── DataInitializationService.cs  # Khởi tạo admin mặc định
└── Program.cs                         # Đăng ký DI và khởi tạo dữ liệu
```

## Lợi ích đã đạt được

1. **Tính nhất quán**: Tất cả User được tạo với Role/Claims mặc định đúng
2. **Tách biệt logic**: Logic tạo User tách khỏi business logic
3. **Dễ bảo trì**: Thay đổi Role/Claims chỉ cần sửa ở một nơi (UserFactory)
4. **Dễ test**: Có thể test UserFactory độc lập
5. **Mở rộng**: Dễ dàng thêm loại User mới (chỉ cần thêm factory method)
6. **Tránh lỗi**: Không thể tạo User với Role sai vì đã được hardcode trong Factory

## Ví dụ sử dụng

### Tạo Customer

```csharp
var model = new RegisterViewModel
{
    FullName = "Nguyễn Văn A",
    Email = "customer@example.com",
    Password = "123456",
    Phone = "0123456789",
    Address = "123 Street"
};

var passwordHash = _authService.HashPassword(model.Password);
var customer = _userFactory.CreateCustomer(model, passwordHash);
// customer sẽ có Role mặc định: "Customer"
```

### Tạo Staff

```csharp
var model = new CreateStaffViewModel
{
    Username = "staff1",
    FullName = "Trần Thị B",
    Password = "123456",
    Role = "Staff"
};

var passwordHash = _authService.HashPassword(model.Password);
var staff = _userFactory.CreateStaff(model, passwordHash);
// staff sẽ có Role = "Staff"
```

### Tạo Admin

```csharp
var model = new CreateStaffViewModel
{
    Username = "admin2",
    FullName = "Lê Văn C",
    Password = "123456",
    Role = "Admin"
};

var passwordHash = _authService.HashPassword(model.Password);
var admin = _userFactory.CreateAdmin(model, passwordHash);
// admin sẽ có Role = "Admin"
```

### Tạo Admin mặc định

```csharp
var passwordHash = _authService.HashPassword("admin123");
var defaultAdmin = _userFactory.CreateAdmin(
    username: "admin",
    passwordHash: passwordHash,
    fullName: "Administrator",
    role: "Admin"
);
```

## Mở rộng trong tương lai

Có thể dễ dàng thêm các factory methods mới:

```csharp
// Ví dụ: Tạo Manager với Role = "Manager"
public Admin CreateManager(CreateStaffViewModel model, string passwordHash)
{
    return new Admin
    {
        Username = model.Username,
        PasswordHash = passwordHash,
        FullName = model.FullName,
        Role = "Manager" // Role mới
    };
}

// Ví dụ: Tạo Customer với VIP status
public Customer CreateVipCustomer(RegisterViewModel model, string passwordHash)
{
    var customer = CreateCustomer(model, passwordHash);
    // Có thể thêm logic đặc biệt cho VIP
    return customer;
}
```

## Kết luận

✅ **Bước 5 đã hoàn thành đầy đủ:**
- IUserFactory interface đã được tạo với các factory methods
- UserFactory class đã triển khai Factory Method Pattern
- AuthService đã sử dụng UserFactory cho đăng ký và tạo staff/admin
- DataInitializationService sử dụng UserFactory để tạo admin mặc định
- UserFactory đã được đăng ký trong DI container
- Admin mặc định được khởi tạo tự động khi khởi động ứng dụng

**Factory Method Pattern đã được triển khai thành công và sẵn sàng sử dụng!**
