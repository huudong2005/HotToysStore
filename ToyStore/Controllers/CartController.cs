using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ToyStore.Helpers;
using ToyStore.Models;
using ToyStore.Services;
using ToyStore.Domain.Interfaces;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Controllers
{
    public class CartController : Controller
    {
        private readonly ToyStoreContext _context;
        private readonly DiscountService _discountService;
        private readonly ISessionService _sessionService;
        private readonly ICartStorageService _cartStorage;
        private readonly IUnitOfWork _unitOfWork;
        private readonly GhnSettings _ghnSettings;

        public CartController(
            ToyStoreContext context,
            DiscountService discountService,
            ISessionService sessionService,
            ICartStorageService cartStorage,
            IUnitOfWork unitOfWork,
            IOptions<GhnSettings> ghnSettings)
        {
            _context = context;
            _discountService = discountService;
            _sessionService = sessionService;
            _cartStorage = cartStorage;
            _unitOfWork = unitOfWork;
            _ghnSettings = ghnSettings.Value;
        }

        public async Task<IActionResult> Index()
        {
            var cart = await _cartStorage.GetCartAsync(HttpContext);
            cart.EnsureCartItemIds();
            await _cartStorage.SaveCartAsync(HttpContext, cart);
            ViewBag.DiscountStrategies = _discountService.GetAllStrategies();
            ViewBag.IsLoggedIn = _sessionService.IsCustomer(HttpContext);

            // Danh sách mã khuyến mãi đang hoạt động để khách chọn & áp dụng nhanh.
            try
            {
                ViewBag.ActivePromotions = await _unitOfWork.Promotions.GetActivePromotionsAsync();
            }
            catch
            {
                // Không để lỗi danh sách voucher chặn việc hiển thị giỏ hàng.
                ViewBag.ActivePromotions = Enumerable.Empty<ToyStore.Domain.Entities.Promotion>();
            }

            // Khôi phục mã khuyến mãi đã áp dụng (nếu có) để hiển thị lại sau khi reload trang.
            ViewBag.AppliedPromoCode = HttpContext.Session.GetString(PromoSessionHelper.AppliedPromoCodeKey);
            var storedDiscount = HttpContext.Session.GetString(PromoSessionHelper.DiscountValueKey);
            decimal appliedDiscount = 0m;
            if (!string.IsNullOrEmpty(storedDiscount))
            {
                decimal.TryParse(storedDiscount, NumberStyles.Any, CultureInfo.InvariantCulture, out appliedDiscount);
            }
            // Tránh giảm vượt quá giá trị giỏ hàng (khi giỏ hàng đã thay đổi).
            if (appliedDiscount > cart.Subtotal) appliedDiscount = cart.Subtotal;
            ViewBag.AppliedDiscountValue = appliedDiscount;

            return View(cart);
        }

        [HttpPost]
        public async Task<IActionResult> Add(int productId, int quantity = 1)
        {
            try
            {
                var product = await _context.Products
                    .Include(p => p.Category)
                    .FirstOrDefaultAsync(p => p.ProductId == productId);

                if (product == null)
                {
                    TempData["ErrorMessage"] = "Sản phẩm không tồn tại";
                    return RedirectToAction("Index", "Home");
                }

                if (product.Status != true)
                {
                    TempData["ErrorMessage"] = "Sản phẩm hiện không được bán";
                    return RedirectToAction("Index", "Home");
                }

                if (quantity <= 0)
                {
                    TempData["ErrorMessage"] = "Số lượng phải lớn hơn 0";
                    return RedirectToAction("Index", "Home");
                }

                if (product.Stock < quantity)
                {
                    TempData["ErrorMessage"] = $"Số lượng sản phẩm không đủ. Chỉ còn {product.Stock} sản phẩm trong kho";
                    return RedirectToAction("Index", "Home");
                }

                var cart = await _cartStorage.GetCartAsync(HttpContext);
                cart.AddItem(product, quantity);
                await _cartStorage.SaveCartAsync(HttpContext, cart);

                TempData["SuccessMessage"] = $"Đã thêm {product.ProductName} vào giỏ hàng";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Update(int productId, int quantity)
        {
            var wantsJson = WantsJsonResponse();

            try
            {
                if (quantity < 1)
                {
                    return wantsJson
                        ? Json(new { success = false, message = "Số lượng phải lớn hơn 0." })
                        : RedirectToAction("Index");
                }

                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                {
                    return wantsJson
                        ? Json(new { success = false, message = "Sản phẩm không tồn tại." })
                        : CartRedirect("Sản phẩm không tồn tại", false);
                }

                if (product.Stock < quantity)
                {
                    var stockMsg = $"Số lượng sản phẩm không đủ. Chỉ còn {product.Stock} sản phẩm trong kho.";
                    return wantsJson
                        ? Json(new { success = false, message = stockMsg, maxStock = product.Stock })
                        : CartRedirect(stockMsg, false);
                }

                var cart = await _cartStorage.GetCartAsync(HttpContext);
                cart.UpdateQuantity(productId, quantity);
                await _cartStorage.SaveCartAsync(HttpContext, cart);

                var updatedItem = cart.Items.FirstOrDefault(i => i.ProductId == productId);

                if (wantsJson)
                {
                    return Json(new
                    {
                        success = true,
                        message = "Đã cập nhật số lượng.",
                        productId,
                        quantity = updatedItem?.Quantity ?? quantity,
                        lineTotal = updatedItem?.Total ?? 0m,
                        unitPrice = updatedItem?.Price ?? product.Price
                    });
                }

                TempData["SuccessMessage"] = "Đã cập nhật giỏ hàng";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                return wantsJson
                    ? Json(new { success = false, message = "Lỗi: " + ex.Message })
                    : CartRedirect("Lỗi: " + ex.Message, false);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Remove(int productId)
        {
            var wantsJson = WantsJsonResponse();

            try
            {
                var cart = await _cartStorage.GetCartAsync(HttpContext);
                var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);

                if (item != null)
                {
                    cart.RemoveItem(productId);
                    await _cartStorage.SaveCartAsync(HttpContext, cart);
                }

                if (wantsJson)
                {
                    return Json(new
                    {
                        success = true,
                        message = item != null
                            ? $"Đã xóa {item.ProductName} khỏi giỏ hàng."
                            : "Đã xóa sản phẩm khỏi giỏ hàng.",
                        productId,
                        isEmpty = !cart.Items.Any()
                    });
                }

                if (item != null)
                {
                    TempData["SuccessMessage"] = $"Đã xóa {item.ProductName} khỏi giỏ hàng";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                return wantsJson
                    ? Json(new { success = false, message = "Lỗi: " + ex.Message })
                    : CartRedirect("Lỗi: " + ex.Message, false);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Clear()
        {
            try
            {
                var cart = new ShoppingCart();
                await _cartStorage.SaveCartAsync(HttpContext, cart);
                TempData["SuccessMessage"] = "Đã xóa toàn bộ giỏ hàng";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> ApplyDiscount(string? discountStrategyName)
        {
            var cart = await _cartStorage.GetCartAsync(HttpContext);

            if (!cart.Items.Any())
            {
                TempData["ErrorMessage"] = "Giỏ hàng trống";
                return RedirectToAction("Index");
            }

            cart.DiscountStrategyName = discountStrategyName;
            await _cartStorage.SaveCartAsync(HttpContext, cart);

            TempData["SuccessMessage"] = "Đã áp dụng khuyến mãi";
            return RedirectToAction("Index");
        }

        // API: Khách hàng nhập mã khuyến mãi (Voucher) tại giỏ hàng.
        [HttpPost]
        public async Task<IActionResult> ApplyPromotion([FromBody] ApplyPromoRequest request)
        {
            try
            {
                var promoCode = request?.PromoCode?.Trim();
                if (string.IsNullOrWhiteSpace(promoCode))
                {
                    return Json(new { success = false, message = "Vui lòng nhập mã khuyến mãi." });
                }

                var cart = await _cartStorage.GetCartAsync(HttpContext);
                cart.EnsureCartItemIds();
                if (!cart.Items.Any())
                {
                    return Json(new { success = false, message = "Giỏ hàng đang trống, không thể áp dụng mã." });
                }

                var selectedIds = request?.SelectedCartItemIds?
                    .Where(id => id > 0)
                    .Distinct()
                    .ToList();

                decimal orderValue;
                if (selectedIds != null && selectedIds.Count > 0)
                {
                    orderValue = cart.GetSubtotal(selectedIds);
                    if (orderValue <= 0)
                    {
                        return Json(new { success = false, message = "Vui lòng chọn ít nhất 1 sản phẩm để áp dụng mã." });
                    }
                }
                else
                {
                    orderValue = cart.Subtotal;
                }

                // Tổng tiền hàng được chọn làm cơ sở tính khuyến mãi.

                var (resultCode, discountValue, message) =
                    await _unitOfWork.Promotions.ApplyPromotionAsync(promoCode, orderValue);

                if (resultCode == 1)
                {
                    // Không cho phép giảm vượt quá tổng tiền hàng.
                    if (discountValue < 0) discountValue = 0;
                    if (discountValue > orderValue) discountValue = orderValue;

                    decimal finalTotal = orderValue - discountValue;

                    // Lưu vào Session để dùng lại lúc Thanh toán.
                    HttpContext.Session.SetString(PromoSessionHelper.AppliedPromoCodeKey, promoCode);
                    HttpContext.Session.SetString(PromoSessionHelper.DiscountValueKey, discountValue.ToString(CultureInfo.InvariantCulture));

                    return Json(new
                    {
                        success = true,
                        discountValue,
                        finalTotal,
                        discountValueText = FormatVnd(discountValue),
                        finalTotalText = FormatVnd(finalTotal),
                        message = string.IsNullOrWhiteSpace(message) ? "Áp dụng mã thành công." : message
                    });
                }

                // Thất bại: xóa mã đã lưu trước đó (nếu có).
                HttpContext.Session.Remove(PromoSessionHelper.AppliedPromoCodeKey);
                HttpContext.Session.Remove(PromoSessionHelper.DiscountValueKey);

                return Json(new
                {
                    success = false,
                    message = string.IsNullOrWhiteSpace(message) ? "Mã khuyến mãi không hợp lệ." : message
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        // Định dạng tiền tệ VNĐ kiểu "50,000đ".
        private static string FormatVnd(decimal amount)
        {
            return amount.ToString("#,##0", CultureInfo.InvariantCulture) + "đ";
        }

        public async Task<IActionResult> Checkout(List<int>? selectedItems)
        {
            if (selectedItems == null || !selectedItems.Any())
            {
                TempData["ErrorMessage"] = "Vui lòng chọn ít nhất 1 sản phẩm.";
                return RedirectToAction(nameof(Index));
            }

            var fullCart = await _cartStorage.GetCartAsync(HttpContext);
            fullCart.EnsureCartItemIds();

            if (!fullCart.Items.Any())
            {
                TempData["ErrorMessage"] = "Giỏ hàng trống, không thể thanh toán";
                return RedirectToAction(nameof(Index));
            }

            var distinctSelected = selectedItems.Where(id => id > 0).Distinct().ToList();
            var cart = fullCart.CreateSubset(distinctSelected);

            if (!cart.Items.Any())
            {
                TempData["ErrorMessage"] = "Không tìm thấy sản phẩm đã chọn trong giỏ hàng.";
                return RedirectToAction(nameof(Index));
            }

            CartSelectionHelper.SaveSelectedIds(HttpContext, distinctSelected);

            ViewBag.Cart = cart;
            ViewBag.DiscountStrategies = _discountService.GetAllStrategies();

            // Lấy giá trị khuyến mãi đã áp dụng từ Session (đảm bảo an toàn khi parse).
            decimal subTotal = cart.Subtotal;
            decimal discountValue = 0m;
            try
            {
                var storedDiscount = HttpContext.Session.GetString(PromoSessionHelper.DiscountValueKey);
                if (!string.IsNullOrWhiteSpace(storedDiscount))
                {
                    decimal.TryParse(storedDiscount, NumberStyles.Any, CultureInfo.InvariantCulture, out discountValue);
                }
            }
            catch
            {
                discountValue = 0m;
            }

            // Không cho phép khuyến mãi (voucher) âm hoặc vượt quá tổng tiền hàng.
            if (discountValue < 0) discountValue = 0m;
            if (discountValue > subTotal) discountValue = subTotal;

            var isLoggedIn = _sessionService.IsCustomer(HttpContext);
            var model = new CheckoutPageViewModel
            {
                IsLoggedIn = isLoggedIn,
                PaymentMethod = "COD"
            };

            // Mặc định: không có ưu đãi hạng thẻ.
            decimal membershipDiscountValue = 0m;
            string? tierName = null;

            if (isLoggedIn)
            {
                var customerId = _sessionService.GetUserId(HttpContext);

                // Include cả Tier để biết DiscountPercent của hạng thẻ.
                var customer = await _unitOfWork.Customers.GetCustomerWithTierAsync(customerId);
                model.Customer = customer;

                // Tính giảm giá VIP nếu khách có hạng thẻ (null checking đầy đủ).
                if (customer?.Tier != null && customer.Tier.DiscountPercent > 0)
                {
                    membershipDiscountValue = subTotal * (customer.Tier.DiscountPercent / 100m);

                    // Phòng thủ: không cho giảm âm hoặc vượt quá tổng tiền hàng.
                    if (membershipDiscountValue < 0) membershipDiscountValue = 0m;
                    if (membershipDiscountValue > subTotal) membershipDiscountValue = subTotal;

                    // Ví dụ hiển thị: "Gold (10%)".
                    tierName = $"{customer.Tier.TierName} ({customer.Tier.DiscountPercent:0.##}%)";
                }
            }
            else
            {
                var savedGuest = GuestOrderSession.GetGuestCheckout(HttpContext);
                if (savedGuest != null)
                {
                    model.Guest = savedGuest;
                }
            }

            // Giảm giá xếp chồng (stackable): tổng cộng = Tạm tính - Ưu đãi hạng thẻ - Voucher.
            decimal finalTotal = subTotal - membershipDiscountValue - discountValue;
            if (finalTotal < 0) finalTotal = 0m;

            ViewBag.SubTotal = subTotal;
            ViewBag.DiscountValue = discountValue;
            ViewBag.MembershipDiscountValue = membershipDiscountValue;
            ViewBag.TierName = tierName;
            ViewBag.FinalTotal = finalTotal;
            ViewBag.FallbackShippingFee = 30_000m;
            ViewBag.MaxCodAmount = _ghnSettings.MaxCodAmount;
            ViewBag.GhnCodLimitMessage = GhnCodHelper.GetLargeOrderNoticeMessage(_ghnSettings.MaxCodAmount);
            ViewBag.GhnCodConfirmMessage = GhnCodHelper.GetLargeOrderConfirmMessage();

            return View("~/Views/Checkout/Index.cshtml", model);
        }

        public async Task<IActionResult> GetCount()
        {
            var cart = await _cartStorage.GetCartAsync(HttpContext);
            return Json(new { count = cart.ItemCount });
        }

        private bool WantsJsonResponse()
        {
            var accept = Request.Headers.Accept.ToString();
            return accept.Contains("application/json", StringComparison.OrdinalIgnoreCase);
        }

        private IActionResult CartRedirect(string message, bool success)
        {
            if (success)
            {
                TempData["SuccessMessage"] = message;
            }
            else
            {
                TempData["ErrorMessage"] = message;
            }

            return RedirectToAction("Index");
        }
    }

    // Body của request áp mã khuyến mãi gửi từ trang giỏ hàng.
    public class ApplyPromoRequest
    {
        public string? PromoCode { get; set; }

        public List<int>? SelectedCartItemIds { get; set; }
    }
}
