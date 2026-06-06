using ToyStore.Domain.Entities;
using ToyStore.Domain.Interfaces;
using ToyStore.Models;
using ToyStore.Services;

namespace ToyStore.Infrastructure.Facades;

/// <summary>
/// Facade Pattern: Gộp các bước phức tạp của quy trình thanh toán thành một interface đơn giản
/// </summary>
public class CheckoutFacade : ICheckoutFacade
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly DiscountService _discountService;
    private readonly IPaymentGateway _paymentGateway;

    public CheckoutFacade(IUnitOfWork unitOfWork, DiscountService discountService, IPaymentGateway paymentGateway)
    {
        _unitOfWork = unitOfWork;
        _discountService = discountService;
        _paymentGateway = paymentGateway;
    }

    /// <summary>
    /// Thực hiện quy trình đặt hàng hoàn chỉnh
    /// </summary>
    public async Task<Order> PlaceOrderAsync(ShoppingCart cart, int customerId, string? paymentMethod = null)
    {
        // Bước 1: Kiểm tra giỏ hàng
        if (cart == null || !cart.Items.Any())
        {
            throw new InvalidOperationException("Giỏ hàng trống, không thể đặt hàng");
        }

        // Bước 2: Kiểm tra tồn kho cho tất cả sản phẩm
        await ValidateStockAvailabilityAsync(cart);

        // Bước 3: Tính giá (sử dụng Strategy Pattern)
        var (subtotal, discountValue, finalTotal) = CalculatePricing(cart);

        // Bước 4: Tạo đơn hàng và cập nhật stock (sử dụng Unit of Work với Transaction)
        var order = await CreateOrderWithTransactionAsync(
            cart, 
            customerId, 
            subtotal, 
            discountValue, 
            finalTotal, 
            paymentMethod
        );

        // Bước 5: Xử lý thanh toán thông qua Adapter Pattern (IPaymentGateway)
        // Với phương thức COD, có thể bỏ qua thanh toán online.
        if (!string.IsNullOrWhiteSpace(paymentMethod) && !string.Equals(paymentMethod, "COD", StringComparison.OrdinalIgnoreCase))
        {
            var paymentResult = await _paymentGateway.ProcessPaymentAsync(order, finalTotal, paymentMethod);
            if (!paymentResult.Success)
            {
                throw new InvalidOperationException($"Thanh toán thất bại: {paymentResult.Message}");
            }
        }

        return order;
    }

    /// <summary>
    /// Bước 1: Kiểm tra tồn kho cho tất cả sản phẩm trong giỏ hàng
    /// </summary>
    private async Task ValidateStockAvailabilityAsync(ShoppingCart cart)
    {
        foreach (var item in cart.Items)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId);
            
            if (product == null)
            {
                throw new InvalidOperationException($"Sản phẩm với ID {item.ProductId} không tồn tại");
            }

            if (product.Status != true)
            {
                throw new InvalidOperationException($"Sản phẩm {product.ProductName} hiện không được bán");
            }

            if (product.Stock < item.Quantity)
            {
                throw new InvalidOperationException(
                    $"Số lượng sản phẩm {product.ProductName} không đủ. Chỉ còn {product.Stock} sản phẩm trong kho"
                );
            }
        }
    }

    /// <summary>
    /// Bước 2: Tính giá sử dụng Strategy Pattern
    /// </summary>
    private (decimal Subtotal, decimal DiscountValue, decimal FinalTotal) CalculatePricing(ShoppingCart cart)
    {
        // Subtotal = ∑(Quantity × UnitPrice)
        decimal subtotal = cart.Subtotal;

        // Tính discount value từ strategy
        var (discountValue, finalTotal) = _discountService.CalculateDiscountAndTotal(
            subtotal,
            cart.DiscountStrategyName
        );

        return (subtotal, discountValue, finalTotal);
    }

    /// <summary>
    /// Bước 3 & 4: Tạo đơn hàng và cập nhật stock trong transaction
    /// </summary>
    private async Task<Order> CreateOrderWithTransactionAsync(
        ShoppingCart cart,
        int customerId,
        decimal subtotal,
        decimal discountValue,
        decimal finalTotal,
        string? paymentMethod)
    {
        // Begin transaction để đảm bảo atomicity
        await _unitOfWork.BeginTransactionAsync();

        try
        {
            var finalPaymentMethod = string.IsNullOrWhiteSpace(paymentMethod) ? "COD" : paymentMethod!;
            var finalDeliveryMethod = "Standard";
            var finalDiscountStrategy = string.IsNullOrWhiteSpace(cart.DiscountStrategyName) ? "NoDiscount" : cart.DiscountStrategyName!;

            // Bước 3a: Tạo đơn hàng qua Oracle procedure
            int newOrderId = await _unitOfWork.Orders.CreateOrderHeaderViaProcedureAsync(
                customerId,
                finalTotal,
                finalPaymentMethod,
                finalDeliveryMethod,
                subtotal,
                discountValue,
                finalDiscountStrategy
            );

            // Bước 3b: Tạo order details và trừ stock qua Oracle procedure
            foreach (var item in cart.Items)
            {
                int resultCode = await _unitOfWork.Orders.CreateOrderDetailViaProcedureAsync(
                    newOrderId,
                    item.ProductId,
                    item.Quantity,
                    item.Price
                );

                if (resultCode != 1)
                {
                    var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId);
                    string productName = product?.ProductName ?? $"ID {item.ProductId}";
                    throw new InvalidOperationException($"Sản phẩm {productName} không đủ tồn kho để thanh toán.");
                }
            }

            // Commit transaction
            await _unitOfWork.CommitTransactionAsync();

            var newOrder = await _unitOfWork.Orders.GetByIdAsync(newOrderId);
            if (newOrder == null)
            {
                throw new InvalidOperationException("Không thể tải đơn hàng vừa tạo.");
            }
            return newOrder;
        }
        catch
        {
            // Rollback nếu có lỗi
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }
}
