using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ToyStore.Domain.Entities;
using ToyStore.Domain.Interfaces;
using ToyStore.Attributes;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Controllers
{
    [AuthorizeRole("Admin", "Staff")]
    public class CategoriesController : Controller
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly ToyStoreContext _context;

        public CategoriesController(ICategoryRepository categoryRepository, ToyStoreContext context)
        {
            _categoryRepository = categoryRepository;
            _context = context; // Giữ lại để hỗ trợ các tác vụ query nhanh nếu cần
        }

        // GET: Categories
        public async Task<IActionResult> Index(string searchString)
        {
            ViewData["CurrentFilter"] = searchString;

            // Tận dụng DBContext để thực hiện tìm kiếm kèm Include danh sách sản phẩm để đếm số lượng
            var categoriesQuery = _context.Categories
                .Include(c => c.Products)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                categoriesQuery = categoriesQuery.Where(c => c.CategoryName.Contains(searchString));
            }

            return View(await categoriesQuery.OrderBy(c => c.CategoryName).ToListAsync());
        }

        // GET: Categories/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var category = await _categoryRepository.GetCategoryWithProductsAsync(id.Value);
            if (category == null) return NotFound();

            return View(category);
        }

        // GET: Categories/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Categories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CategoryId,CategoryName")] Category category)
        {
            if (string.IsNullOrEmpty(category.CategoryName))
            {
                TempData["ErrorMessage"] = "Tên danh mục không được để trống";
                return RedirectToAction(nameof(Index));
            }

            ModelState.Remove("Products");

            if (ModelState.IsValid)
            {
                try
                {
                    await _categoryRepository.AddCategoryViaProcedureAsync(category);
                    TempData["SuccessMessage"] = "Thêm danh mục mới thành công!";
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "Lỗi khi thêm danh mục: " + ex.Message;
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Dữ liệu danh mục không hợp lệ.";
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Categories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();
            return View(category);
        }

        // POST: Categories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("CategoryId,CategoryName")] Category category)
        {
            if (id != category.CategoryId) return NotFound();

            if (string.IsNullOrEmpty(category.CategoryName))
            {
                TempData["ErrorMessage"] = "Tên danh mục không được để trống";
                return RedirectToAction(nameof(Index));
            }

            ModelState.Remove("Products");

            if (ModelState.IsValid)
            {
                try
                {
                    await _categoryRepository.UpdateCategoryViaProcedureAsync(category);
                    TempData["SuccessMessage"] = "Cập nhật danh mục thành công!";
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "Lỗi khi cập nhật: " + ex.Message;
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Dữ liệu danh mục không hợp lệ.";
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Categories/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var category = await _context.Categories
                .Include(c => c.Products)
                .FirstOrDefaultAsync(m => m.CategoryId == id);

            if (category == null) return NotFound();

            return View(category);
        }

        // POST: Categories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                // Gọi Procedure kiểm tra khóa ngoại trước khi thực hiện xóa
                int resultCode = await _categoryRepository.DeleteCategoryViaProcedureAsync(id);

                if (resultCode == 1)
                {
                    TempData["SuccessMessage"] = "Xóa danh mục thành công khỏi hệ thống!";
                }
                else if (resultCode == 2)
                {
                    TempData["ErrorMessage"] = "Không thể xóa! Danh mục này đang chứa sản phẩm của cửa hàng.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi hệ thống: " + ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }
    }
}