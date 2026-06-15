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
        private readonly IProductRepository _productRepository;
        private readonly IWebHostEnvironment _env;

        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        private const long MaxFileSize = 10 * 1024 * 1024; // 10MB

        public ProductsController(
            ToyStoreContext context,
            IProductRepository productRepository,
            IWebHostEnvironment env)
        {
            _context = context;
            _productRepository = productRepository;
            _env = env;
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
        public async Task<IActionResult> Create(Product product, IFormFile? imageFile)
        {
            ModelState.Remove("Category");
            ModelState.Remove("CartItems");
            ModelState.Remove("OrderDetails");
            ModelState.Remove(nameof(Product.ImageUrl));
            ModelState.Remove("imageFile");

            if (imageFile != null && imageFile.Length > 0)
            {
                var (ok, path, error) = await SaveImageAsync(imageFile);
                if (!ok)
                {
                    ModelState.AddModelError("imageFile", error);
                }
                else
                {
                    product.ImageUrl = path;
                }
            }

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
        public async Task<IActionResult> Edit(int id, Product product, IFormFile? imageFile)
        {
            try
            {
                var existingProduct = await _context.Products.FindAsync(id);
                if (existingProduct == null)
                {
                    TempData["ErrorMessage"] = "Sản phẩm không tồn tại";
                    return RedirectToAction("Index");
                }

                if (string.IsNullOrEmpty(product.ProductName))
                {
                    TempData["ErrorMessage"] = "Tên sản phẩm không được để trống";
                    ViewBag.Categories = await _context.Categories.ToListAsync();
                    product.ImageUrl = existingProduct.ImageUrl;
                    return View(product);
                }

                if (product.CategoryId <= 0)
                {
                    TempData["ErrorMessage"] = "Vui lòng chọn danh mục";
                    ViewBag.Categories = await _context.Categories.ToListAsync();
                    product.ImageUrl = existingProduct.ImageUrl;
                    return View(product);
                }

                ModelState.Remove("Category");
                ModelState.Remove("CartItems");
                ModelState.Remove("OrderDetails");
                ModelState.Remove(nameof(Product.ImageUrl));
                ModelState.Remove("imageFile");

                product.ProductId = id;
                product.ImageUrl = existingProduct.ImageUrl;

                if (imageFile != null && imageFile.Length > 0)
                {
                    var (ok, path, error) = await SaveImageAsync(imageFile);
                    if (!ok)
                    {
                        ModelState.AddModelError("imageFile", error);
                        ViewBag.Categories = await _context.Categories.ToListAsync();
                        return View(product);
                    }

                    DeletePhysicalImage(existingProduct.ImageUrl);
                    product.ImageUrl = path;
                }

                if (ModelState.IsValid)
                {
                    await _productRepository.UpdateProductViaProcedureAsync(product);

                    TempData["SuccessMessage"] = "Cập nhật sản phẩm thành công qua Stored Procedure!";
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi: " + ex.Message;
            }

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
                var uploadDir = Path.Combine(webRoot, "images", "products");

                if (!Directory.Exists(uploadDir))
                {
                    Directory.CreateDirectory(uploadDir);
                }

                var fileName = Guid.NewGuid().ToString() + ext;
                var fullPath = Path.Combine(uploadDir, fileName);

                await using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                return (true, $"/images/products/{fileName}", string.Empty);
            }
            catch (Exception ex)
            {
                return (false, string.Empty, "Lỗi khi lưu ảnh: " + ex.Message);
            }
        }

        private void DeletePhysicalImage(string? imagePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(imagePath)) return;
                if (!imagePath.StartsWith("/images/products/", StringComparison.OrdinalIgnoreCase)) return;

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