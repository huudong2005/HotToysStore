using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ToyStore.Attributes;
using ToyStore.Domain.Entities;
using ToyStore.Domain.Interfaces;
using ToyStore.Infrastructure.Data;
using ToyStore.Models;
using ToyStore.Services;

namespace ToyStore.Controllers;

[AuthorizeRole("Admin", "Staff")]
public class PosController : Controller
{
    private const string PosWalkInEmail = "pos-walkin@toystore.local";
    private const string StatusCompleted = "Hoàn thành";
    private static readonly HashSet<string> AllowedPaymentMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "Tiền mặt",
        "Chuyển khoản (QR)"
    };

    private readonly ToyStoreContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PosController> _logger;

    public PosController(
        ToyStoreContext context,
        IUnitOfWork unitOfWork,
        ILogger<PosController> logger)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public IActionResult Index()
    {
        ViewData["Title"] = "Bán hàng tại quầy (POS)";
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> SearchProducts(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Json(Array.Empty<object>());
        }

        var keywordLower = query.Trim().ToLower();

        var suggestions = await _context.Products
            .AsNoTracking()
            .Where(p => p.Status == true
                && p.ProductName.ToLower().Contains(keywordLower))
            .OrderBy(p => p.ProductName)
            .Take(8)
            .Select(p => new
            {
                p.ProductId,
                p.ProductName,
                ImageUrl = p.ImageUrl ?? string.Empty,
                p.Price,
                p.Stock
            })
            .ToListAsync();

        return Json(suggestions);
    }

    [HttpGet]
    public async Task<IActionResult> LookupCustomer(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return Json(new
            {
                found = false,
                message = "Vui lòng nhập số điện thoại."
            });
        }

        var normalized = phone.Trim();
        var customer = await _context.Customers
            .AsNoTracking()
            .Include(c => c.Tier)
            .Where(c => c.Phone != null && c.Phone.Trim() == normalized)
            .FirstOrDefaultAsync();

        if (customer == null)
        {
            return Json(new
            {
                found = false,
                message = "Không tìm thấy khách hàng với SĐT này."
            });
        }

        if (customer.IsLocked)
        {
            return Json(new
            {
                found = false,
                message = "Tài khoản khách hàng đang bị khóa."
            });
        }

        var discountPercent = customer.Tier?.DiscountPercent ?? 0m;
        var tierName = customer.Tier?.TierName ?? "Chưa có hạng";

        return Json(new
        {
            found = true,
            customerId = customer.CustomerId,
            fullName = customer.FullName,
            phone = customer.Phone,
            tierName,
            discountPercent,
            totalCompletedOrders = customer.TotalCompletedOrders
        });
    }

    [HttpPost]
    public async Task<IActionResult> CreatePosOrder([FromBody] PosOrderModel model)
    {
        if (model?.Items == null || model.Items.Count == 0)
        {
            return Json(new { success = false, message = "Giỏ hàng trống, không thể thanh toán." });
        }

        if (string.IsNullOrWhiteSpace(model.PaymentMethod)
            || !AllowedPaymentMethods.Contains(model.PaymentMethod.Trim()))
        {
            return Json(new { success = false, message = "Phương thức thanh toán không hợp lệ." });
        }

        var paymentMethod = model.PaymentMethod.Trim();
        var lineItems = model.Items
            .Where(i => i.ProductId > 0 && i.Quantity > 0)
            .GroupBy(i => i.ProductId)
            .Select(g => new PosOrderItemModel
            {
                ProductId = g.Key,
                Quantity = g.Sum(x => x.Quantity),
                UnitPrice = g.First().UnitPrice
            })
            .ToList();

        if (lineItems.Count == 0)
        {
            return Json(new { success = false, message = "Danh sách sản phẩm không hợp lệ." });
        }

        try
        {
            var productIds = lineItems.Select(i => i.ProductId).Distinct().ToList();
            var products = await _context.Products
                .Where(p => productIds.Contains(p.ProductId))
                .ToDictionaryAsync(p => p.ProductId);

            decimal subtotal = 0;
            foreach (var item in lineItems)
            {
                if (!products.TryGetValue(item.ProductId, out var product))
                {
                    return Json(new { success = false, message = $"Sản phẩm #{item.ProductId} không tồn tại." });
                }

                if (product.Status != true)
                {
                    return Json(new { success = false, message = $"Sản phẩm \"{product.ProductName}\" hiện không được bán." });
                }

                if (product.Stock < item.Quantity)
                {
                    return Json(new
                    {
                        success = false,
                        message = $"\"{product.ProductName}\" không đủ tồn kho (còn {product.Stock})."
                    });
                }

                var unitPrice = product.Price;
                item.UnitPrice = unitPrice;
                subtotal += unitPrice * item.Quantity;
            }

            var (customerId, isWalkIn) = await ResolveCustomerAsync(model.CustomerId);
            decimal membershipDiscount = 0;

            if (!isWalkIn)
            {
                var customer = await _unitOfWork.Customers.GetCustomerWithTierAsync(customerId);
                if (customer?.Tier != null && customer.Tier.DiscountPercent > 0)
                {
                    membershipDiscount = subtotal * (customer.Tier.DiscountPercent / 100m);
                }
            }

            var totalAmount = subtotal - membershipDiscount;
            if (totalAmount < 0)
            {
                totalAmount = 0;
            }

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var orderId = await _unitOfWork.Orders.CreateOrderHeaderViaProcedureAsync(
                    customerId,
                    totalAmount,
                    paymentMethod,
                    "POS",
                    subtotal,
                    0m,
                    "NoDiscount");

                foreach (var item in lineItems)
                {
                    var resultCode = await _unitOfWork.Orders.CreateOrderDetailViaProcedureAsync(
                        orderId,
                        item.ProductId,
                        item.Quantity,
                        item.UnitPrice);

                    if (resultCode != 1)
                    {
                        var name = products[item.ProductId].ProductName;
                        throw new InvalidOperationException($"Sản phẩm \"{name}\" không đủ tồn kho để thanh toán.");
                    }
                }

                var order = await _unitOfWork.Orders.GetByIdAsync(orderId)
                    ?? throw new InvalidOperationException("Không thể tải đơn hàng vừa tạo.");

                order.Status = StatusCompleted;
                order.OrderType = "POS";
                order.ShippingAddress = "Mua tại quầy";
                order.MembershipDiscountValue = membershipDiscount;
                order.ShippingFee = 0;

                _unitOfWork.Orders.Update(order);
                await _unitOfWork.SaveChangesAsync();

                if (!isWalkIn)
                {
                    await UpdateMembershipProgressAsync(customerId);
                }

                await _unitOfWork.CommitTransactionAsync();

                return Json(new
                {
                    success = true,
                    message = "Thanh toán POS thành công!",
                    orderId,
                    subtotal,
                    membershipDiscount,
                    totalAmount
                });
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi tạo đơn POS");
            return Json(new { success = false, message = ex.Message });
        }
    }

    private async Task<(int CustomerId, bool IsWalkIn)> ResolveCustomerAsync(int? customerId)
    {
        if (customerId is > 0)
        {
            var exists = await _context.Customers.AsNoTracking()
                .AnyAsync(c => c.CustomerId == customerId.Value && !c.IsLocked);
            if (exists)
            {
                return (customerId.Value, false);
            }
        }

        var walkInId = await GetOrCreatePosWalkInCustomerIdAsync();
        return (walkInId, true);
    }

    private async Task<int> GetOrCreatePosWalkInCustomerIdAsync()
    {
        var existing = await _unitOfWork.Customers.GetCustomerByEmailAsync(PosWalkInEmail);
        if (existing != null)
        {
            return existing.CustomerId;
        }

        var guest = new Customer
        {
            FullName = "Khách vãng lai",
            Email = PosWalkInEmail,
            Phone = "0000000000",
            Address = "Mua tại quầy",
            PasswordHash = GuestCheckoutService.GuestPasswordPrefix + Guid.NewGuid().ToString("N"),
            CreatedAt = DateTime.Now
        };

        await _unitOfWork.Customers.CreateCustomerViaProcedureAsync(guest);
        var created = await _unitOfWork.Customers.GetCustomerByEmailAsync(PosWalkInEmail)
            ?? throw new InvalidOperationException("Không thể tạo khách vãng lai POS.");

        return created.CustomerId;
    }

    private async Task UpdateMembershipProgressAsync(int customerId)
    {
        var customer = await _unitOfWork.Customers.GetByIdAsync(customerId);
        if (customer == null)
        {
            return;
        }

        customer.TotalCompletedOrders += 1;

        var tiers = await _unitOfWork.MembershipTiers.GetAllOrderedByRequiredOrdersDescAsync();
        var qualifiedTier = tiers?.FirstOrDefault(t => customer.TotalCompletedOrders >= t.RequiredOrders);

        if (qualifiedTier != null && customer.TierId != qualifiedTier.TierId)
        {
            customer.TierId = qualifiedTier.TierId;
        }

        _unitOfWork.Customers.Update(customer);
        await _unitOfWork.SaveChangesAsync();
    }
}
