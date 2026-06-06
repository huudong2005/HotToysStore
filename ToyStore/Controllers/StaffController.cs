using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ToyStore.Models;
using ToyStore.Services;
using ToyStore.Attributes;
using ToyStore.Domain.Entities;
using ToyStore.Domain.Interfaces;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Controllers
{
    [AuthorizeRole("Admin")]
    public class StaffController : Controller
    {
        private readonly IAuthService _authService;
        private readonly IAdminRepository _adminRepository;
        private readonly ToyStoreContext _context;
        private readonly ILogger<StaffController> _logger;

        public StaffController(IAuthService authService, IAdminRepository adminRepository, ToyStoreContext context, ILogger<StaffController> logger)
        {
            _authService = authService;
            _adminRepository = adminRepository;
            _context = context;
            _logger = logger;
        }

        // GET: Staff
        public async Task<IActionResult> Index()
        {
            // Lấy danh sách nhân viên loại trừ tài khoản Admin cấp cao nhất
            var staff = await _context.Admins
                .Where(a => a.Role != "Admin")
                .OrderBy(a => a.FullName)
                .ToListAsync();
            return View(staff);
        }

        // GET: Staff/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Staff/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateStaffViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                if (await _authService.IsUsernameExistsAsync(model.Username))
                {
                    ModelState.AddModelError("Username", "Username này đã được sử dụng");
                    return View(model);
                }

                // Hàm này bên trong AuthService đã sửa đổi để gọi Oracle Procedure
                var result = await _authService.CreateStaffAsync(model);
                if (!result)
                {
                    ModelState.AddModelError("", "Có lỗi xảy ra trong quá trình tạo tài khoản nhân viên");
                    return View(model);
                }

                TempData["SuccessMessage"] = "Tạo tài khoản nhân viên thành công!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating staff account");
                // TẠM THỜI SỬA DÒNG NÀY ĐỂ XEM LỖI CHI TIẾT TỪ ORACLE
                string detailedError = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                ModelState.AddModelError("", "Lỗi chi tiết: " + detailedError);
                return View(model);
            }
        }

        // GET: Staff/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var staff = await _context.Admins.FindAsync(id);
            if (staff == null) return NotFound();

            var model = new CreateStaffViewModel
            {
                FullName = staff.FullName ?? "",
                Username = staff.Username,
                Role = staff.Role ?? "Staff"
            };

            return View(model);
        }

        // POST: Staff/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CreateStaffViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                var staff = await _context.Admins.FindAsync(id);
                if (staff == null) return NotFound();

                // Kiểm tra trùng username ngoại trừ chính nó
                var existingStaff = await _context.Admins
                    .FirstOrDefaultAsync(a => a.Username == model.Username && a.AdminId != id);
                if (existingStaff != null)
                {
                    ModelState.AddModelError("Username", "Username này đã được sử dụng");
                    return View(model);
                }

                staff.FullName = model.FullName;
                staff.Username = model.Username;
                staff.Role = model.Role;

                // Thay đổi Password Hash nếu Admin cấp mật khẩu mới
                if (!string.IsNullOrEmpty(model.Password))
                {
                    staff.PasswordHash = _authService.HashPassword(model.Password);
                }

                // Thực hiện gọi Procedure để cập nhật dữ liệu xuống Oracle
                await _adminRepository.UpdateStaffViaProcedureAsync(staff);

                TempData["SuccessMessage"] = "Cập nhật thông tin nhân viên thành công!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating staff account");
                ModelState.AddModelError("", "Có lỗi xảy ra trong quá trình cập nhật thông tin nhân viên");
                return View(model);
            }
        }

        // GET: Staff/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var staff = await _context.Admins.FindAsync(id);
            if (staff == null) return NotFound();
            return View(staff);
        }

        // POST: Staff/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var staff = await _context.Admins.FindAsync(id);
                if (staff == null)
                {
                    TempData["ErrorMessage"] = "Tài khoản nhân viên không tồn tại.";
                    return RedirectToAction("Index");
                }

                // Thực hiện xóa cứng tài khoản nhân viên thông qua Oracle Procedure
                await _adminRepository.DeleteStaffViaProcedureAsync(id);
                TempData["SuccessMessage"] = "Xóa tài khoản nhân viên thành công!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting staff account");
                TempData["ErrorMessage"] = "Có lỗi xảy ra trong quá trình xóa tài khoản nhân viên";
            }

            return RedirectToAction("Index");
        }
    }
}