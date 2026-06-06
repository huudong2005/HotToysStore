using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ToyStore.Attributes;
using ToyStore.Domain.Entities;
using ToyStore.Domain.Interfaces;

namespace ToyStore.Controllers
{
    [AuthorizeRole("Admin")]
    public class PromotionsController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public PromotionsController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // GET: Promotions
        public async Task<IActionResult> Index(string searchString)
        {
            ViewData["CurrentFilter"] = searchString;

            var query = _unitOfWork.Promotions.Query();

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(p =>
                    p.PromotionCode.Contains(searchString) ||
                    p.PromotionName.Contains(searchString));
            }

            var promotions = await query
                .OrderByDescending(p => p.PromotionId)
                .ToListAsync();

            return View(promotions);
        }

        // GET: Promotions/Create
        public IActionResult Create()
        {
            // Giá trị mặc định gợi ý cho form
            var promotion = new Promotion
            {
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(7),
                IsActive = true
            };
            return View(promotion);
        }

        // POST: Promotions/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PromotionCode,PromotionName,DiscountType,DiscountValue,MinOrderValue,StartDate,EndDate,UsageLimit,UsedCount,IsActive")] Promotion promotion)
        {
            await ValidateBusinessRulesAsync(promotion);

            if (ModelState.IsValid)
            {
                try
                {
                    await _unitOfWork.Promotions.AddAsync(promotion);
                    await _unitOfWork.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Thêm mã khuyến mãi mới thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "Lỗi khi thêm khuyến mãi: " + ex.Message;
                }
            }
            return View(promotion);
        }

        // GET: Promotions/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var promotion = await _unitOfWork.Promotions.GetByIdAsync(id.Value);
            if (promotion == null) return NotFound();

            return View(promotion);
        }

        // POST: Promotions/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("PromotionId,PromotionCode,PromotionName,DiscountType,DiscountValue,MinOrderValue,StartDate,EndDate,UsageLimit,UsedCount,IsActive")] Promotion promotion)
        {
            if (id != promotion.PromotionId) return NotFound();

            await ValidateBusinessRulesAsync(promotion);

            if (ModelState.IsValid)
            {
                try
                {
                    _unitOfWork.Promotions.Update(promotion);
                    await _unitOfWork.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật mã khuyến mãi thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    var exists = await _unitOfWork.Promotions.AnyAsync(p => p.PromotionId == promotion.PromotionId);
                    if (!exists) return NotFound();
                    throw;
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "Lỗi khi cập nhật: " + ex.Message;
                }
            }
            return View(promotion);
        }

        // POST: Promotions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var promotion = await _unitOfWork.Promotions.GetByIdAsync(id);
                if (promotion == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy mã khuyến mãi cần xóa.";
                    return RedirectToAction(nameof(Index));
                }

                _unitOfWork.Promotions.Remove(promotion);
                await _unitOfWork.SaveChangesAsync();
                TempData["SuccessMessage"] = "Xóa mã khuyến mãi thành công!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi khi xóa: " + ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }

        // Kiểm tra các quy tắc nghiệp vụ trước khi lưu
        private async Task ValidateBusinessRulesAsync(Promotion promotion)
        {
            int? excludeId = promotion.PromotionId > 0 ? promotion.PromotionId : null;
            if (await _unitOfWork.Promotions.IsCodeExistsAsync(promotion.PromotionCode, excludeId))
            {
                ModelState.AddModelError(nameof(Promotion.PromotionCode), "Mã code này đã tồn tại, vui lòng chọn mã khác.");
            }

            if (promotion.EndDate < promotion.StartDate)
            {
                ModelState.AddModelError(nameof(Promotion.EndDate), "Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu.");
            }

            if (promotion.DiscountType == "Percentage" && promotion.DiscountValue > 100)
            {
                ModelState.AddModelError(nameof(Promotion.DiscountValue), "Giảm theo % không được vượt quá 100.");
            }
        }
    }
}
