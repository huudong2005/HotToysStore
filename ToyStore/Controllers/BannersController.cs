using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ToyStore.Attributes;
using ToyStore.Domain.Entities;
using ToyStore.Domain.Interfaces;

namespace ToyStore.Controllers
{
    [AuthorizeRole("Admin")]
    public class BannersController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _env;

        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        private const long MaxFileSize = 10 * 1024 * 1024; // 10MB

        public BannersController(IUnitOfWork unitOfWork, IWebHostEnvironment env)
        {
            _unitOfWork = unitOfWork;
            _env = env;
        }

        // GET: Banners
        public async Task<IActionResult> Index()
        {
            var banners = await _unitOfWork.Banners.Query()
                .OrderByDescending(b => b.IsActive)
                .ThenByDescending(b => b.CreatedAt)
                .ToListAsync();

            return View(banners);
        }

        // GET: Banners/Create
        public IActionResult Create()
        {
            return View(new Banner { IsActive = true });
        }

        // POST: Banners/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,IsActive")] Banner banner, IFormFile imageFile)
        {
            // ImagePath không nhập tay mà sinh ra từ file upload -> loại khỏi ModelState
            ModelState.Remove(nameof(Banner.ImagePath));

            if (imageFile == null || imageFile.Length == 0)
            {
                ModelState.AddModelError("imageFile", "Vui lòng chọn ảnh banner.");
            }

            if (!ModelState.IsValid)
            {
                return View(banner);
            }

            try
            {
                var (ok, path, error) = await SaveImageAsync(imageFile);
                if (!ok)
                {
                    ModelState.AddModelError("imageFile", error);
                    return View(banner);
                }

                banner.ImagePath = path;
                banner.CreatedAt = DateTime.Now;

                await _unitOfWork.Banners.AddAsync(banner);
                await _unitOfWork.SaveChangesAsync();

                TempData["SuccessMessage"] = "Thêm banner mới thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi khi thêm banner: " + ex.Message;
                return View(banner);
            }
        }

        // GET: Banners/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var banner = await _unitOfWork.Banners.GetByIdAsync(id.Value);
            if (banner == null) return NotFound();

            return View(banner);
        }

        // POST: Banners/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("BannerId,Title,ImagePath,IsActive,CreatedAt")] Banner banner, IFormFile? imageFile)
        {
            if (id != banner.BannerId) return NotFound();

            // ImagePath đến từ hidden field / file upload; không validate trực tiếp
            ModelState.Remove(nameof(Banner.ImagePath));
            ModelState.Remove("imageFile");

            if (!ModelState.IsValid)
            {
                return View(banner);
            }

            try
            {
                // Nếu admin chọn ảnh mới -> upload ảnh mới và xóa ảnh cũ
                if (imageFile != null && imageFile.Length > 0)
                {
                    var (ok, path, error) = await SaveImageAsync(imageFile);
                    if (!ok)
                    {
                        ModelState.AddModelError("imageFile", error);
                        return View(banner);
                    }

                    var oldPath = banner.ImagePath;
                    banner.ImagePath = path;
                    DeletePhysicalImage(oldPath);
                }

                _unitOfWork.Banners.Update(banner);
                await _unitOfWork.SaveChangesAsync();

                TempData["SuccessMessage"] = "Cập nhật banner thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                var exists = await _unitOfWork.Banners.AnyAsync(b => b.BannerId == banner.BannerId);
                if (!exists) return NotFound();
                throw;
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi khi cập nhật banner: " + ex.Message;
                return View(banner);
            }
        }

        // POST: Banners/ToggleActive/5 - bật/tắt nhanh trạng thái hiển thị 1 banner ngay tại trang Index
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            try
            {
                var banner = await _unitOfWork.Banners.GetByIdAsync(id);
                if (banner == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy banner.";
                    return RedirectToAction(nameof(Index));
                }

                banner.IsActive = !banner.IsActive;
                _unitOfWork.Banners.Update(banner);
                await _unitOfWork.SaveChangesAsync();

                TempData["SuccessMessage"] = banner.IsActive
                    ? "Đã bật hiển thị banner ngoài trang chủ."
                    : "Đã tắt hiển thị banner.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: Banners/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var banner = await _unitOfWork.Banners.GetByIdAsync(id);
                if (banner == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy banner cần xóa.";
                    return RedirectToAction(nameof(Index));
                }

                _unitOfWork.Banners.Remove(banner);
                await _unitOfWork.SaveChangesAsync();

                DeletePhysicalImage(banner.ImagePath);
                TempData["SuccessMessage"] = "Xóa banner thành công!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi khi xóa banner: " + ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }

        // ===== Helpers =====

        // Lưu file ảnh vật lý vào wwwroot/images/banners với tên ngẫu nhiên (Guid).
        private async Task<(bool Ok, string Path, string Error)> SaveImageAsync(IFormFile imageFile)
        {
            try
            {
                var ext = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                if (string.IsNullOrEmpty(ext) || !AllowedExtensions.Contains(ext))
                {
                    return (false, string.Empty, "Định dạng ảnh không hợp lệ (chỉ chấp nhận jpg, jpeg, png, webp, gif).");
                }

                if (imageFile.Length > MaxFileSize)
                {
                    return (false, string.Empty, "Kích thước ảnh tối đa là 10MB.");
                }

                var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var uploadDir = Path.Combine(webRoot, "images", "banners");

                // Tự tạo thư mục nếu chưa tồn tại
                if (!Directory.Exists(uploadDir))
                {
                    Directory.CreateDirectory(uploadDir);
                }

                var fileName = $"{Guid.NewGuid():N}{ext}";
                var fullPath = Path.Combine(uploadDir, fileName);

                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                // Đường dẫn web tương đối để lưu vào DB
                return (true, $"/images/banners/{fileName}", string.Empty);
            }
            catch (Exception ex)
            {
                return (false, string.Empty, "Lỗi khi lưu ảnh: " + ex.Message);
            }
        }

        // Xóa file ảnh vật lý (bỏ qua lỗi để không chặn nghiệp vụ chính).
        private void DeletePhysicalImage(string? imagePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(imagePath)) return;

                var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var relative = imagePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                var fullPath = Path.Combine(webRoot, relative);

                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }
            }
            catch
            {
                // Bỏ qua lỗi xóa file vật lý.
            }
        }
    }
}
