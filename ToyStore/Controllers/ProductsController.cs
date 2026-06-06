using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using ToyStore.Attributes;
using ToyStore.Domain.Entities;
using ToyStore.Domain.Interfaces;
using ToyStore.Infrastructure.Data;
using ToyStore.Models;

namespace ToyStore.Controllers
{
    [AuthorizeRole("Admin", "Staff")]
    public class ProductsController : Controller
    {
        private readonly ToyStoreContext _context;
        private readonly IProductRepository _productRepository; // Inject Repository vào đây

        public ProductsController(ToyStoreContext context, IProductRepository productRepository)
        {
            _context = context;
            _productRepository = productRepository;
        }

        // GET: Products
        public async Task<IActionResult> Index(string searchName, int? categoryId)
        {
            // Tận dụng các phương thức đã viết trong Repository
            IEnumerable<Product> products;

            if (!string.IsNullOrEmpty(searchName))
            {
                products = await _productRepository.FilterProductsViaProcedureAsync(searchName, null, null, null);
            }
            else if (categoryId.HasValue && categoryId.Value > 0)
            {
                products = await _productRepository.GetProductsByCategoryAsync(categoryId.Value);
            }
            else
            {
                products = await _productRepository.GetAllAsync();
            }

            ViewBag.Categories = await _context.Categories.OrderBy(c => c.CategoryName).ToListAsync();
            ViewBag.SearchName = searchName;
            ViewBag.CategoryId = categoryId;

            return View(products);
        }

        // GET: Products/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.ProductId == id);
            
            if (product == null) return NotFound();
            return View(product);
        }

        // GET: Products/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = await _context.Categories.ToListAsync();
            return View();
        }

        // POST: Products/Create - SỬ DỤNG PROCEDURE QUA REPOSITORY
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product)
        {
            // Loại bỏ kiểm tra Validation cho object Category vì chúng ta chỉ cần CategoryId
            ModelState.Remove("Category");
            ModelState.Remove("CartItems");
            ModelState.Remove("OrderDetails");

            if (ModelState.IsValid)
            {
                try
                {
                    await _productRepository.AddProductViaProcedureAsync(product);
                    TempData["SuccessMessage"] = "Thêm sản phẩm thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "Lỗi database: " + ex.Message;
                }
            }

            // Nếu lỗi Validation (như thiếu tên sản phẩm), load lại Category cho Dropdown
            ViewBag.Categories = await _context.Categories.ToListAsync();
            return View(product);
        }

        // GET: Products/Edit/5
        public async Task<IActionResult> Edit(int id) // Giữ nguyên tham số 'id' như cũ của bạn
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            var categories = await _context.Categories.ToListAsync();
            ViewBag.Categories = categories;
            return View(product);
        }

        // POST: Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product product)
        {
            try
            {
                // 1. RÀNBUỘC 1: Kiểm tra sản phẩm có tồn tại hay không
                var existingProduct = await _context.Products.FindAsync(id);
                if (existingProduct == null)
                {
                    TempData["ErrorMessage"] = "Sản phẩm không tồn tại";
                    return RedirectToAction("Index");
                }

                // 2. RÀNBUỘC 2: Kiểm tra tên sản phẩm không được để trống
                if (string.IsNullOrEmpty(product.ProductName))
                {
                    TempData["ErrorMessage"] = "Tên sản phẩm không được để trống";
                    ViewBag.Categories = await _context.Categories.ToListAsync();
                    return View(product);
                }

                // 3. RÀNBUỘC 3: Kiểm tra tính hợp lệ của danh mục
                if (product.CategoryId <= 0)
                {
                    TempData["ErrorMessage"] = "Vui lòng chọn danh mục";
                    ViewBag.Categories = await _context.Categories.ToListAsync();
                    return View(product);
                }

                // Loại bỏ Validation cho các Object liên kết để tránh lỗi 400 như file Product.cs thiết lập
                ModelState.Remove("Category");
                ModelState.Remove("CartItems");
                ModelState.Remove("OrderDetails");

                if (ModelState.IsValid)
                {
                    // Thay vì dùng _context.Products.Update(existingProduct) như cũ,
                    // Chúng ta gọi Procedure của Oracle để thực hiện cập nhật
                    await _productRepository.UpdateProductViaProcedureAsync(product);

                    TempData["SuccessMessage"] = "Cập nhật sản phẩm thành công qua Stored Procedure!";
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
            }

            // Nếu có lỗi xảy ra hoặc ModelState không hợp lệ, trả về View kèm danh sách Categories
            ViewBag.Categories = await _context.Categories.ToListAsync();
            return View(product);
        }

        // GET: Products/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.ProductId == id);

            if (product == null) return NotFound();
            return View(product);
        }

        // POST: Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                // 1. Kiểm tra xem sản phẩm thực sự tồn tại trước khi ra lệnh xóa không
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    TempData["ErrorMessage"] = "Sản phẩm không tồn tại hoặc đã bị xóa trước đó.";
                    return RedirectToAction("Index");
                }

                // 2. Gọi Procedure xử lý xóa/ẩn và lấy kết quả trả về
                int resultCode = await _productRepository.DeleteProductViaProcedureAsync(id);

                // 3. Đưa ra thông báo dựa theo logic Oracle đã xử lý
                if (resultCode == 1)
                {
                    TempData["SuccessMessage"] = "Xóa sản phẩm thành công khỏi hệ thống!";
                }
                else if (resultCode == 2)
                {
                    TempData["SuccessMessage"] = "Sản phẩm đã phát sinh đơn hàng hoặc giỏ hàng nên hệ thống tự động chuyển trạng thái thành 'Ngừng bán'.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi khi xử lý xóa sản phẩm: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}