using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ToyStore.Attributes;
using ToyStore.Domain.Entities;
using ToyStore.Domain.Events;
using ToyStore.Domain.Interfaces;
using ToyStore.Helpers;
using ToyStore.Models;
using ToyStore.Services;

namespace ToyStore.Controllers
{
    public class OrderController : Controller
    {
        private readonly ICheckoutFacade _checkoutFacade;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISessionService _sessionService;
        private readonly IOrderEventDispatcher _orderEventDispatcher;
        private readonly IGuestCheckoutService _guestCheckoutService;
        private readonly ICartStorageService _cartStorage;

        public OrderController(
            ICheckoutFacade checkoutFacade,
            IUnitOfWork unitOfWork,
            ISessionService sessionService,
            IOrderEventDispatcher orderEventDispatcher,
            IGuestCheckoutService guestCheckoutService,
            ICartStorageService cartStorage)
        {
            _checkoutFacade = checkoutFacade;
            _unitOfWork = unitOfWork;
            _sessionService = sessionService;
            _orderEventDispatcher = orderEventDispatcher;
            _guestCheckoutService = guestCheckoutService;
            _cartStorage = cartStorage;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            string paymentMethod,
            string? guestFullName,
            string? guestEmail,
            string? guestPhone,
            string? guestAddress)
        {
            try
            {
                var cart = await _cartStorage.GetCartAsync(HttpContext);
                if (!cart.Items.Any())
                {
                    TempData["ErrorMessage"] = "Giỏ hàng trống, không thể thanh toán.";
                    return RedirectToAction("Index", "Cart");
                }

                var customerId = await ResolveCustomerIdForCheckoutAsync(
                    guestFullName, guestEmail, guestPhone, guestAddress);

                if (string.Equals(paymentMethod, "VNPAY", StringComparison.OrdinalIgnoreCase))
                {
                    return RedirectToAction("CreatePaymentUrlGet", "Payment");
                }

                var createdOrder = await _checkoutFacade.PlaceOrderAsync(cart, customerId, paymentMethod);

                await _cartStorage.ClearCartAfterOrderAsync(HttpContext, customerId);

                await _orderEventDispatcher.PublishAsync(new OrderConfirmedEvent(createdOrder));

                if (!_sessionService.IsCustomer(HttpContext))
                {
                    GuestOrderSession.GrantOrderAccess(HttpContext, createdOrder.OrderId);
                    GuestOrderSession.ClearGuestCheckout(HttpContext);
                    TempData["SuccessMessage"] = $"Đặt hàng thành công! Mã đơn hàng: #{createdOrder.OrderId}";
                    return RedirectToAction(nameof(Confirmation), new { id = createdOrder.OrderId });
                }

                TempData["SuccessMessage"] = $"Đặt hàng thành công! Mã đơn hàng: #{createdOrder.OrderId}";
                return RedirectToAction(nameof(Details), new { id = createdOrder.OrderId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction("Checkout", "Cart");
            }
        }

        [AuthorizeRole("Customer")]
        public async Task<IActionResult> Details(int id)
        {
            var order = await _unitOfWork.Orders.GetOrderWithDetailsAsync(id);
            var customerId = _sessionService.GetUserId(HttpContext);

            if (order == null || order.CustomerId != customerId)
            {
                return NotFound();
            }

            return View(order);
        }

        /// <summary>
        /// Trang xác nhận đơn cho khách vãng lai (hoặc sau thanh toán).
        /// </summary>
        public async Task<IActionResult> Confirmation(int id)
        {
            var order = await _unitOfWork.Orders.GetOrderWithDetailsAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            var customerId = _sessionService.GetUserId(HttpContext);
            if (_sessionService.IsCustomer(HttpContext))
            {
                if (order.CustomerId != customerId)
                {
                    return NotFound();
                }

                return View(order);
            }

            if (!GuestOrderSession.CanAccessOrder(HttpContext, id, 0))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền xem đơn hàng này.";
                return RedirectToAction("Index", "Home");
            }

            return View(order);
        }

        [HttpPost]
        public async Task<IActionResult> BuyNow(int productId, int quantity = 1)
        {
            try
            {
                var product = await _unitOfWork.Products.GetProductWithCategoryAsync(productId);

                if (product == null)
                {
                    TempData["ErrorMessage"] = "Sản phẩm không tồn tại";
                    return RedirectToAction("Index", "Home");
                }

                if (product.Status != true)
                {
                    TempData["ErrorMessage"] = "Sản phẩm hiện không được bán";
                    return RedirectToAction("ProductDetails", "Home", new { id = productId });
                }

                if (product.Stock < quantity)
                {
                    TempData["ErrorMessage"] = $"Số lượng sản phẩm không đủ. Chỉ còn {product.Stock} sản phẩm trong kho";
                    return RedirectToAction("ProductDetails", "Home", new { id = productId });
                }

                if (quantity <= 0)
                {
                    TempData["ErrorMessage"] = "Số lượng phải lớn hơn 0";
                    return RedirectToAction("ProductDetails", "Home", new { id = productId });
                }

                var customerId = _sessionService.GetUserId(HttpContext);
                if (customerId == 0)
                {
                    var cart = await _cartStorage.GetCartAsync(HttpContext);
                    cart.Clear();
                    cart.AddItem(product, quantity);
                    await _cartStorage.SaveCartAsync(HttpContext, cart);
                    TempData["SuccessMessage"] = "Đã thêm sản phẩm vào giỏ. Vui lòng nhập thông tin giao hàng để hoàn tất đơn.";
                    return RedirectToAction("Checkout", "Cart");
                }

                await _unitOfWork.BeginTransactionAsync();

                try
                {
                    decimal subtotal = product.Price * quantity;
                    decimal discountValue = 0;
                    decimal finalTotal = subtotal;

                    var newOrder = new Order
                    {
                        CustomerId = customerId,
                        OrderDate = DateTime.Now,
                        Status = "Pending",
                        Subtotal = subtotal,
                        DiscountValue = discountValue,
                        TotalAmount = finalTotal,
                        DiscountStrategyName = "NoDiscount",
                        PaymentMethod = "COD"
                    };

                    await _unitOfWork.Orders.AddAsync(newOrder);
                    await _unitOfWork.SaveChangesAsync();

                    var orderDetail = new OrderDetail
                    {
                        OrderId = newOrder.OrderId,
                        ProductId = product.ProductId,
                        Quantity = quantity,
                        UnitPrice = product.Price
                    };

                    await _unitOfWork.OrderDetails.AddAsync(orderDetail);

                    product.Stock -= quantity;
                    if (product.Stock < 0)
                    {
                        product.Stock = 0;
                    }

                    _unitOfWork.Products.Update(product);

                    await _unitOfWork.SaveChangesAsync();
                    await _orderEventDispatcher.PublishAsync(new OrderConfirmedEvent(newOrder));
                    await _unitOfWork.CommitTransactionAsync();

                    TempData["SuccessMessage"] = $"Mua ngay thành công! Mã đơn hàng: #{newOrder.OrderId}";
                    return RedirectToAction("Details", new { id = newOrder.OrderId });
                }
                catch
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
                return RedirectToAction("ProductDetails", "Home", new { id = productId });
            }
        }

        [AuthorizeRole("Customer")]
        public async Task<IActionResult> MyOrders()
        {
            var customerId = _sessionService.GetUserId(HttpContext);
            var orders = await _unitOfWork.Orders.GetOrdersByCustomerIdAsync(customerId);

            return View(orders);
        }

        [HttpPost]
        [AuthorizeRole("Customer")]
        public async Task<IActionResult> Cancel(int id)
        {
            try
            {
                var order = await _unitOfWork.Orders.GetOrderWithDetailsAsync(id);
                var customerId = _sessionService.GetUserId(HttpContext);

                if (order == null || order.CustomerId != customerId)
                {
                    TempData["ErrorMessage"] = "Đơn hàng không tồn tại";
                    return RedirectToAction("MyOrders");
                }

                if (!order.CanCancel())
                {
                    var stateName = order.GetState().StateName;
                    TempData["ErrorMessage"] =
                        $"Không thể hủy đơn hàng ở trạng thái {stateName}. Chỉ có thể hủy đơn hàng ở trạng thái Pending hoặc Confirmed.";
                    return RedirectToAction("MyOrders");
                }

                await _unitOfWork.BeginTransactionAsync();

                try
                {
                    foreach (var detail in order.OrderDetails)
                    {
                        var product = await _unitOfWork.Products.GetByIdAsync(detail.ProductId);
                        if (product != null)
                        {
                            product.Stock += detail.Quantity;
                            _unitOfWork.Products.Update(product);
                        }
                    }

                    order.Cancel();
                    _unitOfWork.Orders.Update(order);
                    await _unitOfWork.SaveChangesAsync();

                    await _orderEventDispatcher.PublishAsync(new OrderCancelledEvent(order));
                    await _unitOfWork.CommitTransactionAsync();

                    TempData["SuccessMessage"] = "Đã hủy đơn hàng thành công";
                    return RedirectToAction("MyOrders");
                }
                catch
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
                return RedirectToAction("MyOrders");
            }
        }

        private async Task<int> ResolveCustomerIdForCheckoutAsync(
            string? guestFullName,
            string? guestEmail,
            string? guestPhone,
            string? guestAddress)
        {
            if (_sessionService.IsCustomer(HttpContext))
            {
                return _sessionService.GetUserId(HttpContext);
            }

            var guestInfo = new GuestCheckoutInfo
            {
                FullName = guestFullName ?? string.Empty,
                Email = guestEmail ?? string.Empty,
                Phone = guestPhone ?? string.Empty,
                Address = guestAddress ?? string.Empty
            };

            if (!TryValidateGuestInfo(guestInfo, out var validationError))
            {
                throw new InvalidOperationException(validationError);
            }

            GuestOrderSession.SaveGuestCheckout(HttpContext, guestInfo);
            return await _guestCheckoutService.ResolveCustomerIdAsync(HttpContext, guestInfo);
        }

        private static bool TryValidateGuestInfo(GuestCheckoutInfo info, out string error)
        {
            if (string.IsNullOrWhiteSpace(info.FullName))
            {
                error = "Vui lòng nhập họ tên người nhận.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(info.Email) || !info.Email.Contains('@'))
            {
                error = "Vui lòng nhập email hợp lệ.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(info.Phone))
            {
                error = "Vui lòng nhập số điện thoại.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(info.Address))
            {
                error = "Vui lòng nhập địa chỉ giao hàng.";
                return false;
            }

            error = string.Empty;
            return true;
        }

    }
}
