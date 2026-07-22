using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using ToyStore.Attributes;
using ToyStore.Domain.Entities;
using ToyStore.Domain.Events;
using ToyStore.Domain.Interfaces;
using ToyStore.Helpers;
using ToyStore.Hubs;
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
        private readonly IHubContext<SupportHub> _hubContext;
        private readonly GhnOrderShippingService _ghnOrderShippingService;
        private readonly ILogger<OrderController> _logger;

        public OrderController(
            ICheckoutFacade checkoutFacade,
            IUnitOfWork unitOfWork,
            ISessionService sessionService,
            IOrderEventDispatcher orderEventDispatcher,
            IGuestCheckoutService guestCheckoutService,
            ICartStorageService cartStorage,
            IHubContext<SupportHub> hubContext,
            GhnOrderShippingService ghnOrderShippingService,
            ILogger<OrderController> logger)
        {
            _checkoutFacade = checkoutFacade;
            _unitOfWork = unitOfWork;
            _sessionService = sessionService;
            _orderEventDispatcher = orderEventDispatcher;
            _guestCheckoutService = guestCheckoutService;
            _cartStorage = cartStorage;
            _hubContext = hubContext;
            _ghnOrderShippingService = ghnOrderShippingService;
            _logger = logger;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            string paymentMethod,
            string? guestFullName,
            string? guestEmail,
            string? guestPhone,
            string? guestAddress,
            string? deliveryStreet,
            int? ghnProvinceId,
            int? ghnDistrictId,
            string? ghnWardCode,
            string? shippingAddress,
            decimal shippingFee = 0)
        {
            try
            {
                var cart = await _cartStorage.GetCartAsync(HttpContext);
                if (!cart.Items.Any())
                {
                    TempData["ErrorMessage"] = "Giỏ hàng trống, không thể thanh toán.";
                    return RedirectToAction("Index", "Cart");
                }

                cart.EnsureCartItemIds();
                var selectedIds = CartSelectionHelper.GetSelectedIds(HttpContext);
                if (!selectedIds.Any())
                {
                    TempData["ErrorMessage"] = "Vui lòng chọn ít nhất 1 sản phẩm.";
                    return RedirectToAction("Index", "Cart");
                }

                var fullCart = cart;
                cart = fullCart.CreateSubset(selectedIds);
                if (!cart.Items.Any())
                {
                    TempData["ErrorMessage"] = "Không tìm thấy sản phẩm đã chọn trong giỏ hàng.";
                    return RedirectToAction("Index", "Cart");
                }

                PromoSessionHelper.ApplySessionPromoToCart(HttpContext, cart);
                PromoSessionHelper.CapDiscountToSubtotal(cart, HttpContext);

                if (!_sessionService.IsCustomer(HttpContext) && !string.IsNullOrWhiteSpace(deliveryStreet))
                {
                    guestAddress = deliveryStreet;
                }

                var customerId = await ResolveCustomerIdForCheckoutAsync(
                    guestFullName, guestEmail, guestPhone, guestAddress);

                var normalizedShippingFee = shippingFee < 0 ? 0 : shippingFee;

                if (string.Equals(paymentMethod, "VNPAY", StringComparison.OrdinalIgnoreCase))
                {
                    PendingGhnCheckoutSession.SaveDraft(HttpContext, new PendingGhnCheckoutData
                    {
                        GhnProvinceId = ghnProvinceId,
                        GhnDistrictId = ghnDistrictId,
                        GhnWardCode = ghnWardCode,
                        ShippingAddress = shippingAddress,
                        DeliveryStreet = deliveryStreet,
                        ShippingFee = normalizedShippingFee
                    });

                    return RedirectToAction("CreatePaymentUrlGet", "Payment");
                }

                var normalizedShippingFeeCod = normalizedShippingFee;
                var deliveryMethod = normalizedShippingFeeCod > 0 || ghnDistrictId is > 0 ? "GHN" : "Standard";

                var createdOrder = await _checkoutFacade.PlaceOrderAsync(
                    cart,
                    customerId,
                    paymentMethod,
                    normalizedShippingFeeCod,
                    deliveryMethod);

                // Áp dụng ưu đãi hạng thẻ thành viên (xếp chồng): gán MembershipDiscountValue
                // và trừ thêm vào tổng tiền của đơn trước khi hoàn tất.
                await ApplyMembershipDiscountAsync(createdOrder, cart);

                await SaveOrderShippingAddressAsync(
                    createdOrder,
                    shippingAddress,
                    deliveryStreet,
                    ghnProvinceId,
                    ghnDistrictId,
                    ghnWardCode);

                try
                {
                    await TryCreateGhnShippingAsync(
                        createdOrder,
                        customerId,
                        guestFullName,
                        guestPhone,
                        guestAddress,
                        deliveryStreet,
                        ghnDistrictId,
                        ghnWardCode,
                        new PendingGhnCheckoutData
                        {
                            GhnProvinceId = ghnProvinceId,
                            GhnDistrictId = ghnDistrictId,
                            GhnWardCode = ghnWardCode,
                            ShippingAddress = shippingAddress,
                            DeliveryStreet = deliveryStreet
                        });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "GHN đồng bộ thất bại cho đơn #{OrderId}", createdOrder.OrderId);
                    TempData["GhnError"] =
                        "Đơn hàng đã được ghi nhận trên hệ thống nhưng chưa thể đồng bộ sang Giao Hàng Nhanh vì: "
                        + ex.Message;
                }

                await PromoSessionHelper.RecordPromoUsageAsync(_unitOfWork, cart);

                await _cartStorage.RemoveItemsAsync(HttpContext, selectedIds, customerId);
                PromoSessionHelper.ClearSessionPromo(HttpContext);

                await _orderEventDispatcher.PublishAsync(new OrderConfirmedEvent(createdOrder));

                await _hubContext.Clients.Group("Admins").SendAsync(
                    "ReceiveAdminNotification",
                    "Đơn hàng mới",
                    $"Đơn hàng #{createdOrder.OrderId} vừa được đặt thành công!",
                    "/Orders");

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

                var cart = await _cartStorage.GetCartAsync(HttpContext);
                cart.Clear();
                cart.AddItem(product, quantity);
                cart.EnsureCartItemIds();
                await _cartStorage.SaveCartAsync(HttpContext, cart);

                PromoSessionHelper.ClearSessionPromo(HttpContext);

                var cartItem = cart.Items.FirstOrDefault(i => i.ProductId == productId);
                if (cartItem == null)
                {
                    TempData["ErrorMessage"] = "Không thể thêm sản phẩm vào giỏ hàng.";
                    return RedirectToAction("ProductDetails", "Home", new { id = productId });
                }

                CartSelectionHelper.SaveSelectedIds(HttpContext, new[] { cartItem.CartItemId });

                TempData["SuccessMessage"] = "Chuẩn bị thanh toán. Vui lòng chọn phương thức thanh toán.";
                return RedirectToAction("Checkout", "Cart", new { selectedItems = new[] { cartItem.CartItemId } });
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

        private async Task ApplyMembershipDiscountAsync(Order order, ShoppingCart cart)
        {
            if (order == null)
            {
                return;
            }

            // Lấy khách hàng kèm hạng thẻ (Include Tier) để biết DiscountPercent.
            var customer = await _unitOfWork.Customers.GetCustomerWithTierAsync(order.CustomerId);
            if (customer?.Tier == null || customer.Tier.DiscountPercent <= 0)
            {
                return; // Khách không có hạng thẻ -> không áp ưu đãi.
            }

            decimal subtotal = cart?.Subtotal ?? order.Subtotal;
            decimal membershipDiscount = subtotal * (customer.Tier.DiscountPercent / 100m);

            // Phòng thủ: chặn giá trị âm và không vượt quá tổng tiền hiện tại của đơn.
            if (membershipDiscount < 0) membershipDiscount = 0m;
            if (membershipDiscount > order.TotalAmount) membershipDiscount = order.TotalAmount;

            if (membershipDiscount <= 0)
            {
                return;
            }

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                order.MembershipDiscountValue = membershipDiscount;
                order.TotalAmount -= membershipDiscount;
                if (order.TotalAmount < 0) order.TotalAmount = 0m;

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

        private async Task SaveOrderShippingAddressAsync(
            Order order,
            string? shippingAddress,
            string? deliveryStreet,
            int? ghnProvinceId,
            int? ghnDistrictId,
            string? ghnWardCode)
        {
            await _ghnOrderShippingService.SaveShippingAddressAsync(order, new PendingGhnCheckoutData
            {
                GhnProvinceId = ghnProvinceId,
                GhnDistrictId = ghnDistrictId,
                GhnWardCode = ghnWardCode,
                ShippingAddress = shippingAddress,
                DeliveryStreet = deliveryStreet
            });
        }

        private async Task TryCreateGhnShippingAsync(
            Order order,
            int customerId,
            string? guestFullName,
            string? guestPhone,
            string? guestAddress,
            string? deliveryStreet,
            int? ghnDistrictId,
            string? ghnWardCode,
            PendingGhnCheckoutData? ghnData = null)
        {
            if (order == null || ghnDistrictId is not > 0 || string.IsNullOrWhiteSpace(ghnWardCode))
            {
                return;
            }

            ghnData ??= new PendingGhnCheckoutData
            {
                GhnDistrictId = ghnDistrictId,
                GhnWardCode = ghnWardCode,
                DeliveryStreet = deliveryStreet
            };

            var customer = await _unitOfWork.Customers.GetByIdAsync(customerId);
            await _ghnOrderShippingService.CreateShippingOrderAsync(
                order,
                customer,
                ghnData,
                guestFullName,
                guestPhone,
                guestAddress);
        }

    }
}
