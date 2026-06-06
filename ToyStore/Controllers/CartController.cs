using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        public CartController(
            ToyStoreContext context,
            DiscountService discountService,
            ISessionService sessionService,
            ICartStorageService cartStorage,
            IUnitOfWork unitOfWork)
        {
            _context = context;
            _discountService = discountService;
            _sessionService = sessionService;
            _cartStorage = cartStorage;
            _unitOfWork = unitOfWork;
        }

        public async Task<IActionResult> Index()
        {
            var cart = await _cartStorage.GetCartAsync(HttpContext);
            ViewBag.DiscountStrategies = _discountService.GetAllStrategies();
            ViewBag.IsLoggedIn = _sessionService.IsCustomer(HttpContext);

            // Khôi phục mã khuyến mãi đã áp dụng (nếu có) để hiển thị lại sau khi reload trang.
            ViewBag.AppliedPromoCode = HttpContext.Session.GetString("AppliedPromoCode");
            var storedDiscount = HttpContext.Session.GetString("DiscountValue");
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
            try
            {
                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                {
                    TempData["ErrorMessage"] = "Sản phẩm không tồn tại";
                    return RedirectToAction("Index");
                }

                if (product.Stock < quantity)
                {
                    TempData["ErrorMessage"] = $"Số lượng sản phẩm không đủ. Chỉ còn {product.Stock} sản phẩm trong kho";
                    return RedirectToAction("Index");
                }

                var cart = await _cartStorage.GetCartAsync(HttpContext);
                cart.UpdateQuantity(productId, quantity);
                await _cartStorage.SaveCartAsync(HttpContext, cart);

                TempData["SuccessMessage"] = "Đã cập nhật giỏ hàng";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Remove(int productId)
        {
            try
            {
                var cart = await _cartStorage.GetCartAsync(HttpContext);
                var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);

                if (item != null)
                {
                    cart.RemoveItem(productId);
                    await _cartStorage.SaveCartAsync(HttpContext, cart);
                    TempData["SuccessMessage"] = $"Đã xóa {item.ProductName} khỏi giỏ hàng";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
                return RedirectToAction("Index");
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
                if (!cart.Items.Any())
                {
                    return Json(new { success = false, message = "Giỏ hàng đang trống, không thể áp dụng mã." });
                }

                // Tổng tiền hàng hiện tại của giỏ làm cơ sở tính khuyến mãi.
                decimal orderValue = cart.Subtotal;

                var (resultCode, discountValue, message) =
                    await _unitOfWork.Promotions.ApplyPromotionAsync(promoCode, orderValue);

                if (resultCode == 1)
                {
                    // Không cho phép giảm vượt quá tổng tiền hàng.
                    if (discountValue < 0) discountValue = 0;
                    if (discountValue > orderValue) discountValue = orderValue;

                    decimal finalTotal = orderValue - discountValue;

                    // Lưu vào Session để dùng lại lúc Thanh toán.
                    HttpContext.Session.SetString("AppliedPromoCode", promoCode);
                    HttpContext.Session.SetString("DiscountValue", discountValue.ToString(CultureInfo.InvariantCulture));

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
                HttpContext.Session.Remove("AppliedPromoCode");
                HttpContext.Session.Remove("DiscountValue");

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

        public async Task<IActionResult> Checkout()
        {
            var cart = await _cartStorage.GetCartAsync(HttpContext);

            if (!cart.Items.Any())
            {
                TempData["ErrorMessage"] = "Giỏ hàng trống, không thể thanh toán";
                return RedirectToAction("Index");
            }

            ViewBag.Cart = cart;
            ViewBag.DiscountStrategies = _discountService.GetAllStrategies();

            var isLoggedIn = _sessionService.IsCustomer(HttpContext);
            var model = new CheckoutPageViewModel
            {
                IsLoggedIn = isLoggedIn,
                PaymentMethod = "COD"
            };

            if (isLoggedIn)
            {
                var customerId = _sessionService.GetUserId(HttpContext);
                model.Customer = await _unitOfWork.Customers.GetByIdAsync(customerId);
            }
            else
            {
                var savedGuest = GuestOrderSession.GetGuestCheckout(HttpContext);
                if (savedGuest != null)
                {
                    model.Guest = savedGuest;
                }
            }

            return View(model);
        }

        public async Task<IActionResult> GetCount()
        {
            var cart = await _cartStorage.GetCartAsync(HttpContext);
            return Json(new { count = cart.ItemCount });
        }
    }

    // Body của request áp mã khuyến mãi gửi từ trang giỏ hàng.
    public class ApplyPromoRequest
    {
        public string? PromoCode { get; set; }
    }
}
