# Bước 3: Strategy Pattern cho Khuyến mãi (Discounts) - Đã Hoàn Thành ✅

## Tổng quan

Dự án đã được triển khai đầy đủ **Strategy Pattern** để quản lý các loại khuyến mãi khác nhau, áp dụng công thức: **Total = ∑(Quantity × UnitPrice) – DiscountValue**

## Cấu trúc đã triển khai

### 1. IDiscountStrategy Interface (Domain/Interfaces)

Interface định nghĩa Strategy cho việc tính toán khuyến mãi:

```csharp
public interface IDiscountStrategy
{
    string StrategyName { get; }
    decimal CalculateDiscount(decimal total);
    decimal CalculateTotal(decimal total);
}
```

**Công thức được áp dụng:**
- `CalculateDiscount(total)`: Tính giá trị khuyến mãi (DiscountValue)
- `CalculateTotal(total)`: Tính tổng tiền sau khi giảm giá = total - discount

### 2. Các Strategy Classes (Domain/Strategies)

#### VipDiscountStrategy (Giảm giá 10%)
- **StrategyName**: "VipDiscount"
- **Logic**: Giảm 10% của tổng tiền
- **Công thức**: `DiscountValue = total × 0.10`

#### SeasonalDiscountStrategy (Giảm cố định 50,000 VNĐ)
- **StrategyName**: "SeasonalDiscount"
- **Logic**: Giảm cố định 50,000 VNĐ, nhưng không vượt quá tổng tiền
- **Công thức**: `DiscountValue = min(50000, total)`

#### NoDiscountStrategy (Không có khuyến mãi)
- **StrategyName**: "NoDiscount"
- **Logic**: Không giảm giá
- **Công thức**: `DiscountValue = 0`

### 3. DiscountStrategyFactory (Domain/Strategies)

Factory class để tạo IDiscountStrategy từ tên strategy:

```csharp
public static class DiscountStrategyFactory
{
    public static IDiscountStrategy CreateStrategy(string? strategyName);
    public static IEnumerable<IDiscountStrategy> GetAllStrategies();
}
```

### 4. DiscountService (Services)

Service để tính toán khuyến mãi và tổng tiền:

```csharp
public class DiscountService
{
    public (decimal DiscountValue, decimal FinalTotal) CalculateDiscountAndTotal(
        decimal subtotal, 
        string? strategyName = null
    );
    
    public IEnumerable<IDiscountStrategy> GetAllStrategies();
}
```

### 5. ShoppingCart (Models)

Đã được cập nhật để hỗ trợ discount:

```csharp
public class ShoppingCart
{
    public string? DiscountStrategyName { get; set; }
    
    // Tổng tiền trước khi giảm giá: ∑(Quantity × UnitPrice)
    public decimal Subtotal => Items.Sum(item => item.Total);
    
    // Giá trị khuyến mãi (DiscountValue)
    public decimal DiscountValue { get; }
    
    // Tổng tiền sau khi giảm giá: Total = Subtotal - DiscountValue
    public decimal Total { get; }
}
```

### 6. Order Entity (Domain/Entities)

Đã được cập nhật để lưu thông tin discount:

```csharp
public partial class Order
{
    public decimal Subtotal { get; set; }              // ∑(Quantity × UnitPrice)
    public decimal DiscountValue { get; set; }          // Giá trị khuyến mãi
    public string? DiscountStrategyName { get; set; }  // Tên strategy
    public decimal TotalAmount { get; set; }            // Total = Subtotal - DiscountValue
}
```

## Áp dụng công thức

### Công thức: Total = ∑(Quantity × UnitPrice) – DiscountValue

#### Trong ShoppingCart:

```csharp
// Subtotal = ∑(Quantity × UnitPrice)
public decimal Subtotal => Items.Sum(item => item.Total);

// DiscountValue được tính từ strategy
public decimal DiscountValue
{
    get
    {
        var strategy = DiscountStrategyFactory.CreateStrategy(DiscountStrategyName);
        return strategy.CalculateDiscount(Subtotal);
    }
}

// Total = Subtotal - DiscountValue
public decimal Total
{
    get
    {
        var strategy = DiscountStrategyFactory.CreateStrategy(DiscountStrategyName);
        return strategy.CalculateTotal(Subtotal);
    }
}
```

#### Trong OrderController.Create:

```csharp
// 1. Tính Subtotal = ∑(Quantity × UnitPrice)
decimal subtotal = cart.Subtotal;

// 2. Tính DiscountValue từ strategy
var (discountValue, finalTotal) = _discountService.CalculateDiscountAndTotal(
    subtotal, 
    cart.DiscountStrategyName
);

// 3. Tạo Order với thông tin đầy đủ
var newOrder = new Order
{
    Subtotal = subtotal,                    // ∑(Quantity × UnitPrice)
    DiscountValue = discountValue,          // DiscountValue
    TotalAmount = finalTotal,               // Total = Subtotal - DiscountValue
    DiscountStrategyName = cart.DiscountStrategyName
};
```

## Sử dụng trong Controllers

### CartController

#### Hiển thị giỏ hàng với discount strategies

```csharp
public IActionResult Index()
{
    var cart = GetCart();
    ViewBag.DiscountStrategies = _discountService.GetAllStrategies();
    return View(cart);
}
```

#### Áp dụng discount strategy

```csharp
[HttpPost]
public IActionResult ApplyDiscount(string? discountStrategyName)
{
    var cart = GetCart();
    cart.DiscountStrategyName = discountStrategyName;
    SaveCart(cart);
    return RedirectToAction("Index");
}
```

### OrderController

#### Tạo đơn hàng với discount

```csharp
[HttpPost]
public async Task<IActionResult> Create(Order order)
{
    var cart = GetCart();
    
    // Tính toán theo công thức: Total = ∑(Quantity × UnitPrice) – DiscountValue
    decimal subtotal = cart.Subtotal;
    var (discountValue, finalTotal) = _discountService.CalculateDiscountAndTotal(
        subtotal, 
        cart.DiscountStrategyName
    );

    var newOrder = new Order
    {
        Subtotal = subtotal,
        DiscountValue = discountValue,
        TotalAmount = finalTotal,
        DiscountStrategyName = cart.DiscountStrategyName
    };
    
    // ... lưu đơn hàng
}
```

## Ví dụ tính toán

### Ví dụ 1: VipDiscount (10%)

**Giỏ hàng:**
- Sản phẩm A: 100,000 VNĐ × 2 = 200,000 VNĐ
- Sản phẩm B: 150,000 VNĐ × 1 = 150,000 VNĐ

**Tính toán:**
- Subtotal = ∑(Quantity × UnitPrice) = 200,000 + 150,000 = **350,000 VNĐ**
- DiscountValue = 350,000 × 10% = **35,000 VNĐ**
- Total = 350,000 - 35,000 = **315,000 VNĐ**

### Ví dụ 2: SeasonalDiscount (50,000 VNĐ)

**Giỏ hàng:**
- Sản phẩm A: 100,000 VNĐ × 2 = 200,000 VNĐ
- Sản phẩm B: 150,000 VNĐ × 1 = 150,000 VNĐ

**Tính toán:**
- Subtotal = ∑(Quantity × UnitPrice) = 200,000 + 150,000 = **350,000 VNĐ**
- DiscountValue = min(50,000, 350,000) = **50,000 VNĐ**
- Total = 350,000 - 50,000 = **300,000 VNĐ**

### Ví dụ 3: SeasonalDiscount với tổng tiền nhỏ hơn 50,000

**Giỏ hàng:**
- Sản phẩm A: 30,000 VNĐ × 1 = 30,000 VNĐ

**Tính toán:**
- Subtotal = **30,000 VNĐ**
- DiscountValue = min(50,000, 30,000) = **30,000 VNĐ** (không vượt quá tổng tiền)
- Total = 30,000 - 30,000 = **0 VNĐ** (tối thiểu là 0)

### Ví dụ 4: NoDiscount

**Giỏ hàng:**
- Sản phẩm A: 100,000 VNĐ × 2 = 200,000 VNĐ

**Tính toán:**
- Subtotal = **200,000 VNĐ**
- DiscountValue = **0 VNĐ**
- Total = 200,000 - 0 = **200,000 VNĐ**

## Cấu trúc thư mục

```
ToyStore/
├── Domain/
│   ├── Interfaces/
│   │   └── IDiscountStrategy.cs          # Interface định nghĩa Strategy
│   └── Strategies/
│       ├── DiscountStrategyFactory.cs    # Factory để tạo Strategy
│       ├── VipDiscountStrategy.cs        # Strategy: Giảm 10%
│       ├── SeasonalDiscountStrategy.cs  # Strategy: Giảm 50k
│       └── NoDiscountStrategy.cs        # Strategy: Không giảm
├── Services/
│   └── DiscountService.cs                # Service tính toán discount
├── Models/
│   └── ShoppingCart.cs                   # Đã cập nhật hỗ trợ discount
├── Domain/Entities/
│   └── Order.cs                          # Đã thêm Subtotal, DiscountValue, DiscountStrategyName
└── Controllers/
    ├── CartController.cs                 # Áp dụng discount strategy
    └── OrderController.cs                # Lưu discount vào Order
```

## Database Schema

Order table đã được cập nhật với các trường mới:
- `Subtotal` (decimal(12,2)): Tổng tiền trước khi giảm giá
- `DiscountValue` (decimal(12,2)): Giá trị khuyến mãi
- `DiscountStrategyName` (nvarchar(50)): Tên strategy được áp dụng

**Lưu ý**: Cần chạy migration để thêm các trường mới vào database:
```bash
dotnet ef migrations add AddDiscountFieldsToOrder
dotnet ef database update
```

## Lợi ích đã đạt được

1. **Linh hoạt**: Dễ dàng thêm strategy mới (chỉ cần tạo class mới implement IDiscountStrategy)
2. **Tách biệt logic**: Logic tính toán discount tách khỏi business logic
3. **Dễ test**: Có thể test từng strategy độc lập
4. **Maintainability**: Dễ bảo trì và mở rộng
5. **Công thức rõ ràng**: Total = ∑(Quantity × UnitPrice) – DiscountValue được áp dụng nhất quán

## Mở rộng trong tương lai

Có thể dễ dàng thêm các strategy mới:

```csharp
// Ví dụ: PercentageDiscountStrategy (giảm theo % tùy chỉnh)
public class PercentageDiscountStrategy : IDiscountStrategy
{
    private readonly decimal _percentage;
    
    public PercentageDiscountStrategy(decimal percentage)
    {
        _percentage = percentage;
    }
    
    public decimal CalculateDiscount(decimal total)
    {
        return total * (_percentage / 100);
    }
}

// Ví dụ: BuyMoreDiscountStrategy (mua càng nhiều giảm càng nhiều)
public class BuyMoreDiscountStrategy : IDiscountStrategy
{
    public decimal CalculateDiscount(decimal total)
    {
        if (total >= 1000000) return total * 0.15m; // 15% nếu >= 1 triệu
        if (total >= 500000) return total * 0.10m;  // 10% nếu >= 500k
        if (total >= 200000) return total * 0.05m;  // 5% nếu >= 200k
        return 0;
    }
}
```

## Kết luận

✅ **Bước 3 đã hoàn thành đầy đủ:**
- IDiscountStrategy interface đã được tạo với CalculateDiscount(decimal total)
- Các strategy classes đã được triển khai: VipDiscountStrategy (10%), SeasonalDiscountStrategy (50k), NoDiscountStrategy
- DiscountStrategyFactory quản lý việc tạo strategies
- DiscountService cung cấp API tính toán discount
- ShoppingCart và Order đã hỗ trợ discount
- CartController và OrderController đã sử dụng Strategy Pattern
- **Công thức Total = ∑(Quantity × UnitPrice) – DiscountValue đã được áp dụng đúng**

**Strategy Pattern đã được triển khai thành công và sẵn sàng sử dụng!**
