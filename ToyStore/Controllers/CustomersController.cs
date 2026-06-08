using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ToyStore.Models;
using ToyStore.Attributes;
using ToyStore.Helpers;
using ToyStore.Services;
using ToyStore.Domain.Interfaces;
using ToyStore.Domain.Entities;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Controllers
{
    public class CustomersController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuthService _authService;

        public CustomersController(IUnitOfWork unitOfWork, IAuthService authService)
        {
            _unitOfWork = unitOfWork;
            _authService = authService;
        }

        // GET: Customers
        [AuthorizeRole("Admin")]
        public async Task<IActionResult> Index()
        {
            return View(await _unitOfWork.Customers.Query().ToListAsync());
        }

        // GET: Customers/Details/5
        [AuthorizeRole("Admin")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Nạp kèm hạng thẻ (Include Tier) để hiển thị trạng thái thành viên.
            var customer = await _unitOfWork.Customers.GetCustomerWithTierAsync(id.Value);
            if (customer == null)
            {
                return NotFound();
            }

            return View(customer);
        }

        // GET: Customers/Create
        [AuthorizeRole("Admin")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Customers/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRole("Admin")]
        public async Task<IActionResult> Create([Bind("CustomerId,FullName,Email,Phone,Address,PasswordHash,CreatedAt")] Customer customer)
        {
            customer.CreatedAt = null; // Oracle procedure tự set CURRENT_TIMESTAMP

            var duplicatedEmailCount = await _unitOfWork.Customers.Query()
                .CountAsync(c => c.Email == customer.Email);
            var duplicatedEmail = duplicatedEmailCount > 0;
            if (duplicatedEmail)
            {
                ModelState.AddModelError("Email", "Email này đã được sử dụng.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Luôn hash mật khẩu khi tạo mới từ màn hình Admin
                    customer.PasswordHash = _authService.HashPassword(customer.PasswordHash);
                    await _unitOfWork.Customers.CreateCustomerViaProcedureAsync(customer);
                    TempData["SuccessMessage"] = "Tạo khách hàng thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "Lỗi khi tạo khách hàng: " + ex.Message;
                }
            }
            return View(customer);
        }

        // GET: Customers/Edit/5
        [AuthorizeRole("Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var customer = await _unitOfWork.Customers.GetByIdAsync(id.Value);
            if (customer == null)
            {
                return NotFound();
            }
            return View(customer);
        }

        // POST: Customers/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRole("Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("CustomerId,FullName,Email,Phone,Address,PasswordHash,CreatedAt")] Customer customer)
        {
            if (id != customer.CustomerId)
            {
                return NotFound();
            }

            var duplicatedEmailCount = await _unitOfWork.Customers.Query()
                .CountAsync(c => c.Email == customer.Email && c.CustomerId != customer.CustomerId);
            var duplicatedEmail = duplicatedEmailCount > 0;
            if (duplicatedEmail)
            {
                ModelState.AddModelError("Email", "Email này đã được sử dụng bởi khách hàng khác.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingCustomer = await _unitOfWork.Customers.GetByIdAsync(customer.CustomerId);
                    if (existingCustomer == null) return NotFound();

                    existingCustomer.FullName = customer.FullName;
                    existingCustomer.Email = customer.Email;
                    existingCustomer.Phone = customer.Phone;
                    existingCustomer.Address = customer.Address;

                    // Nếu admin để trống PasswordHash thì procedure sẽ giữ nguyên mật khẩu cũ.
                    string? newPasswordHash = string.IsNullOrWhiteSpace(customer.PasswordHash)
                        ? null
                        : _authService.HashPassword(customer.PasswordHash);

                    await _unitOfWork.Customers.UpdateCustomerViaProcedureAsync(existingCustomer, newPasswordHash);
                    TempData["SuccessMessage"] = "Cập nhật khách hàng thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "Lỗi khi cập nhật khách hàng: " + ex.Message;
                }
            }
            return View(customer);
        }

        // GET: Customers/Delete/5
        [AuthorizeRole("Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var customer = await _unitOfWork.Customers.Query()
                .FirstOrDefaultAsync(m => m.CustomerId == id);
            if (customer == null)
            {
                return NotFound();
            }

            return View(customer);
        }

        // POST: Customers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [AuthorizeRole("Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                int resultCode = await _unitOfWork.Customers.DeleteCustomerViaProcedureAsync(id);
                if (resultCode == 1)
                {
                    TempData["SuccessMessage"] = $"Xóa khách hàng #{id} thành công!";
                }
                else if (resultCode == 2)
                {
                    TempData["ErrorMessage"] = "Không thể xóa khách hàng vì đã có lịch sử đơn hàng hoặc giỏ hàng.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Xóa khách hàng thất bại do lỗi cơ sở dữ liệu.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi khi xóa khách hàng: " + ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: Customers/ToggleLock/5 — Khóa / mở khóa tài khoản khách hàng.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRole("Admin")]
        public async Task<IActionResult> ToggleLock(int id)
        {
            try
            {
                var customer = await _unitOfWork.Customers.GetByIdAsync(id);
                if (customer == null)
                {
                    if (WantsJsonResponse())
                    {
                        return Json(new { success = false, message = "Khách hàng không tồn tại." });
                    }

                    TempData["ErrorMessage"] = "Khách hàng không tồn tại.";
                    return RedirectToAction(nameof(Index));
                }

                customer.IsLocked = !customer.IsLocked;
                _unitOfWork.Customers.Update(customer);
                await _unitOfWork.SaveChangesAsync();

                var message = customer.IsLocked
                    ? $"Đã khóa tài khoản \"{customer.FullName}\"."
                    : $"Đã mở khóa tài khoản \"{customer.FullName}\".";

                if (WantsJsonResponse())
                {
                    return Json(new { success = true, isLocked = customer.IsLocked, message });
                }

                TempData["SuccessMessage"] = message;
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                if (WantsJsonResponse())
                {
                    return Json(new { success = false, message = "Lỗi: " + ex.Message });
                }

                TempData["ErrorMessage"] = "Lỗi khi thay đổi trạng thái khóa: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        private bool WantsJsonResponse()
        {
            var accept = Request.Headers.Accept.ToString();
            return accept.Contains("application/json", StringComparison.OrdinalIgnoreCase);
        }

        private bool CustomerExists(int id)
        {
            return _unitOfWork.Customers.Query().Count(e => e.CustomerId == id) > 0;
        }

        // Customer Profile Management Actions
        [AuthorizeRole("Customer")]
        public async Task<IActionResult> MyProfile()
        {
            var userSession = AuthHelper.GetCurrentUser(HttpContext);
            if (userSession == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            // Nạp kèm hạng thẻ (Include Tier) để hiển thị trạng thái thành viên.
            var customer = await _unitOfWork.Customers.GetCustomerWithTierAsync(userSession.UserId);
            if (customer == null)
            {
                return NotFound();
            }

            await PopulateMembershipStatusAsync(customer.CustomerId);

            var viewModel = new EditCustomerViewModel
            {
                CustomerId = customer.CustomerId,
                FullName = customer.FullName,
                Email = customer.Email,
                Phone = customer.Phone,
                Address = customer.Address
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRole("Customer")]
        public async Task<IActionResult> MyProfile(EditCustomerViewModel model)
        {
            var userSession = AuthHelper.GetCurrentUser(HttpContext);
            if (userSession == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            if (userSession.UserId != model.CustomerId)
            {
                return Forbid();
            }

            // Luôn nạp trạng thái thành viên để hiển thị đúng khi render lại trang (kể cả khi có lỗi).
            await PopulateMembershipStatusAsync(model.CustomerId);

            // Kiểm tra email có trùng với customer khác không
            var existingCustomer = await _unitOfWork.Customers.Query()
                .FirstOrDefaultAsync(c => c.Email == model.Email && c.CustomerId != model.CustomerId);
            if (existingCustomer != null)
            {
                ModelState.AddModelError("Email", "Email này đã được sử dụng bởi tài khoản khác");
            }

            if (ModelState.IsValid)
            {
                var customer = await _unitOfWork.Customers.GetByIdAsync(model.CustomerId);
                if (customer == null)
                {
                    return NotFound();
                }

                // Cập nhật thông tin cơ bản
                customer.FullName = model.FullName;
                customer.Email = model.Email;
                customer.Phone = model.Phone;
                customer.Address = model.Address;

                // Xử lý đổi mật khẩu nếu có
                string? newPasswordHash = null;
                if (!string.IsNullOrEmpty(model.NewPassword))
                {
                    if (string.IsNullOrEmpty(model.CurrentPassword))
                    {
                        ModelState.AddModelError("CurrentPassword", "Vui lòng nhập mật khẩu hiện tại để đổi mật khẩu");
                        return View(model);
                    }

                    // Kiểm tra mật khẩu hiện tại
                    if (!_authService.VerifyPassword(model.CurrentPassword, customer.PasswordHash))
                    {
                        ModelState.AddModelError("CurrentPassword", "Mật khẩu hiện tại không đúng");
                        return View(model);
                    }

                    // Cập nhật mật khẩu mới
                    newPasswordHash = _authService.HashPassword(model.NewPassword);
                }

                try
                {
                    await _unitOfWork.Customers.UpdateCustomerViaProcedureAsync(customer, newPasswordHash);
                    TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
                    return RedirectToAction(nameof(MyProfile));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "Lỗi khi cập nhật thông tin: " + ex.Message;
                }
            }

            return View(model);
        }

        /// <summary>
        /// Tính toán & đẩy thông tin trạng thái hạng thành viên ra ViewBag cho trang hồ sơ:
        /// hạng hiện tại, % ưu đãi, số đơn đã hoàn thành và tiến độ lên hạng kế tiếp.
        /// </summary>
        private async Task PopulateMembershipStatusAsync(int customerId)
        {
            // Mặc định: tài khoản thường (chưa có hạng).
            ViewBag.HasTier = false;
            ViewBag.TierName = null;
            ViewBag.DiscountPercent = 0m;
            ViewBag.TotalCompletedOrders = 0;
            ViewBag.NextTierName = null;
            ViewBag.OrdersToNextTier = 0;

            var customer = await _unitOfWork.Customers.GetCustomerWithTierAsync(customerId);
            if (customer == null)
            {
                return;
            }

            int completed = customer.TotalCompletedOrders;
            ViewBag.TotalCompletedOrders = completed;

            if (customer.Tier != null)
            {
                ViewBag.HasTier = true;
                ViewBag.TierName = customer.Tier.TierName;
                ViewBag.DiscountPercent = customer.Tier.DiscountPercent;
            }

            // Tìm hạng kế tiếp: hạng có RequiredOrders nhỏ nhất nhưng vẫn lớn hơn số đơn hiện tại.
            var tiers = await _unitOfWork.MembershipTiers.GetAllOrderedByRequiredOrdersDescAsync();
            var nextTier = tiers?
                .Where(t => t.RequiredOrders > completed)
                .OrderBy(t => t.RequiredOrders)
                .FirstOrDefault();

            if (nextTier != null)
            {
                ViewBag.NextTierName = nextTier.TierName;
                ViewBag.OrdersToNextTier = nextTier.RequiredOrders - completed;
            }
        }

    }
}
