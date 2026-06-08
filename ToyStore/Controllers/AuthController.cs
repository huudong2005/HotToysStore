using Microsoft.AspNetCore.Mvc;
using ToyStore.Models;
using ToyStore.Services;
using ToyStore.Domain.Interfaces;
using System.Diagnostics;

namespace ToyStore.Controllers
{
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;
        private readonly ISessionService _sessionService;
        private readonly ICartStorageService _cartStorage;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IAuthService authService,
            ISessionService sessionService,
            ICartStorageService cartStorage,
            IUnitOfWork unitOfWork,
            ILogger<AuthController> logger)
        {
            _authService = authService;
            _sessionService = sessionService;
            _cartStorage = cartStorage;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (_sessionService.IsAuthenticated(HttpContext))
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Thử đăng nhập với tất cả loại tài khoản
                UserSession? userSession = null;
                
                // Thử đăng nhập Customer trước
                userSession = await _authService.LoginAsync(model.EmailOrUsername, model.Password, "Customer");

                // Mật khẩu đúng nhưng tài khoản bị khóa -> thông báo riêng (không thử Admin).
                if (userSession == null)
                {
                    var customer = await _unitOfWork.Customers.GetCustomerByEmailAsync(model.EmailOrUsername);
                    if (customer != null
                        && _authService.VerifyPassword(model.Password, customer.PasswordHash)
                        && customer.IsLocked)
                    {
                        ModelState.AddModelError(string.Empty,
                            "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên.");
                        return View(model);
                    }
                }

                // Nếu không thành công, thử Admin/Staff
                if (userSession == null)
                {
                    userSession = await _authService.LoginAsync(model.EmailOrUsername, model.Password, "Admin");
                }

                if (userSession == null)
                {
                    ModelState.AddModelError("", "Email/Username hoặc mật khẩu không đúng");
                    return View(model);
                }

                _sessionService.SetUserSession(HttpContext, userSession);

                if (userSession.UserType == "Customer")
                {
                    await _cartStorage.RestoreCartAfterLoginAsync(HttpContext, userSession.UserId);
                }

                _logger.LogInformation($"User {userSession.Username} logged in successfully");

                // Redirect dựa trên loại user
                return userSession.UserType switch
                {
                    "Admin" => RedirectToAction("Index", "Home"),
                    "Staff" => RedirectToAction("Index", "Home"),
                    "Customer" => RedirectToAction("Index", "Home"),
                    _ => RedirectToAction("Index", "Home")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                ModelState.AddModelError("", "Có lỗi xảy ra trong quá trình đăng nhập");
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (_sessionService.IsAuthenticated(HttpContext))
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Kiểm tra email đã tồn tại chưa
                if (await _authService.IsEmailExistsAsync(model.Email))
                {
                    ModelState.AddModelError("Email", "Email này đã được sử dụng");
                    return View(model);
                }

                var result = await _authService.RegisterAsync(model);
                
                if (!result)
                {
                    ModelState.AddModelError("", "Có lỗi xảy ra trong quá trình đăng ký");
                    return View(model);
                }

                TempData["SuccessMessage"] = "Đăng ký thành công! Bạn có thể đăng nhập ngay bây giờ.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration");
                ModelState.AddModelError("", "Có lỗi xảy ra trong quá trình đăng ký");
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            if (_sessionService.IsCustomer(HttpContext))
            {
                var customerId = _sessionService.GetUserId(HttpContext);
                if (customerId > 0)
                {
                    await _cartStorage.PersistCartBeforeLogoutAsync(HttpContext, customerId);
                }
            }

            _sessionService.ClearSession(HttpContext);
            TempData["SuccessMessage"] = "Đăng xuất thành công";
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
