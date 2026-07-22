using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ToyStore.Models;
using ToyStore.Helpers;
using ToyStore.Domain.Entities;
using ToyStore.Domain.Interfaces;
using ToyStore.Infrastructure.Data;
using ToyStore.Attributes;
using ToyStore.Services;

namespace ToyStore.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ToyStoreContext _context;
        private readonly ISessionService _sessionService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRecommendationService _recommendationService;

        public HomeController(
            ILogger<HomeController> logger,
            ToyStoreContext context,
            ISessionService sessionService,
            IUnitOfWork unitOfWork,
            IRecommendationService recommendationService)
        {
            _logger = logger;
            _context = context;
            _sessionService = sessionService;
            _unitOfWork = unitOfWork;
            _recommendationService = recommendationService;
        }

        [TrackActivity]
        public async Task<IActionResult> Index(string searchName)
        {
            var user = _sessionService.GetUserSession(HttpContext);
            ViewBag.User = user;

            // Lấy danh sách banner đang kích hoạt cho Hero Carousel (không để lỗi DB làm sập trang)
            try
            {
                ViewBag.ActiveBanners = await _unitOfWork.Banners.GetActiveBannersAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không tải được banner trang chủ");
                ViewBag.ActiveBanners = null;
            }

            var showWelcomeToast = HttpContext.Session.GetString("ShowWelcomeToast");
            if (!string.IsNullOrEmpty(showWelcomeToast))
            {
                HttpContext.Session.Remove("ShowWelcomeToast");
                TempData["ShowWelcomeToast"] = "true";
            }

            // Sửa lỗi mapping bool? và tránh ORA-00904 bằng cách dùng == true
            // Lưu ý: Phải có .HasConversion<int>() trong Context để Oracle hiểu == true là = 1
            var categoriesQuery = _context.Categories
                .Include(c => c.Products.Where(p => p.Status == true))
                .Where(c => c.Products.Any(p => p.Status == true))
                .AsQueryable();

            var categoriesWithProducts = await categoriesQuery
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

            if (!string.IsNullOrEmpty(searchName))
            {
                await FilterCategoriesBySearchAsync(categoriesWithProducts, searchName);
            }

            ViewBag.SearchName = searchName;
            return View(categoriesWithProducts);
        }

        [TrackActivity]
        public async Task<IActionResult> Shop(string? keyword, int? categoryId, decimal? minPrice, decimal? maxPrice)
        {
            var user = _sessionService.GetUserSession(HttpContext);
            ViewBag.User = user;

            // Gọi Stored Procedure để vừa Tìm kiếm vừa Lọc (danh mục + khoảng giá)
            var products = (await _unitOfWork.Products.FilterProductsViaProcedureAsync(keyword, categoryId, minPrice, maxPrice))
                .Where(p => p.Status == true)
                .ToList();

            // Lấy toàn bộ danh mục để dựng dropdown lọc
            var allCategories = await _unitOfWork.Categories.GetAllAsync();
            var orderedCategories = allCategories.OrderBy(c => c.CategoryName).ToList();

            // Gán Category cho từng sản phẩm để hiển thị tên danh mục trên thẻ
            var categoryLookup = orderedCategories.ToDictionary(c => c.CategoryId);
            foreach (var product in products)
            {
                if (categoryLookup.TryGetValue(product.CategoryId, out var category))
                {
                    product.Category = category;
                }
            }

            // Dropdown danh mục: tự đánh dấu option đang chọn theo categoryId hiện tại
            ViewBag.Categories = new SelectList(orderedCategories, "CategoryId", "CategoryName", categoryId);

            // Lưu lại trạng thái bộ lọc để bind lại vào Form trên UI
            ViewBag.Keyword = keyword;
            ViewBag.CategoryId = categoryId;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;

            return View(products);
        }

        [TrackActivity]
        public async Task<IActionResult> ProductDetails(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductImages.OrderBy(pi => pi.DisplayOrder))
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
            {
                return NotFound();
            }

            var user = _sessionService.GetUserSession(HttpContext);
            ViewBag.User = user;

            try
            {
                var recommendedIds = await _recommendationService.GetRecommendedProductIdsAsync(id, 4);
                if (recommendedIds.Count > 0)
                {
                    var recommendedProducts = await _context.Products
                        .AsNoTracking()
                        .Include(p => p.Category)
                        .Where(p => recommendedIds.Contains(p.ProductId) && p.Status == true)
                        .ToListAsync();

                    // Giữ đúng thứ tự confidence từ Apriori.
                    ViewBag.RecommendedProducts = recommendedIds
                        .Select(rid => recommendedProducts.FirstOrDefault(p => p.ProductId == rid))
                        .Where(p => p != null)
                        .Cast<Product>()
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không tải được gợi ý sản phẩm Apriori cho ProductId={ProductId}", id);
            }

            return View(product);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private async Task FilterCategoriesBySearchAsync(ICollection<Category> categories, string keyword)
        {
            var matchedProductIds = (await _unitOfWork.Products.FilterProductsViaProcedureAsync(keyword, null, null, null))
                .Where(p => p.Status == true)
                .Select(p => p.ProductId)
                .ToHashSet();

            var categoriesToRemove = new List<Category>();
            foreach (var category in categories)
            {
                category.Products = category.Products
                    .Where(p => matchedProductIds.Contains(p.ProductId))
                    .ToList();

                if (!category.Products.Any())
                {
                    categoriesToRemove.Add(category);
                }
            }

            foreach (var category in categoriesToRemove)
            {
                categories.Remove(category);
            }
        }

        [HttpGet]
        public async Task<IActionResult> SearchSuggestions(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return Json(Array.Empty<object>());
            }

            var keyword = query.Trim();
            var keywordLower = keyword.ToLower();

            var suggestions = await _context.Products
                .AsNoTracking()
                .Where(p => p.Status == true
                    && p.ProductName.ToLower().Contains(keywordLower))
                .OrderBy(p => p.ProductName)
                .Take(5)
                .Select(p => new
                {
                    p.ProductId,
                    p.ProductName,
                    ImageUrl = p.ImageUrl ?? string.Empty,
                    p.Price
                })
                .ToListAsync();

            return Json(suggestions);
        }
    }
}