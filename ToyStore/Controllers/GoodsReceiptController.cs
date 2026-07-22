using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ToyStore.Attributes;
using ToyStore.Domain.Entities;
using ToyStore.Domain.Interfaces;
using ToyStore.Infrastructure.Data;
using ToyStore.Models;

namespace ToyStore.Controllers;

[AuthorizeRole("Admin")]
public class GoodsReceiptController : Controller
{
    private readonly ToyStoreContext _context;
    private readonly ISessionService _sessionService;
    private readonly ILogger<GoodsReceiptController> _logger;

    public GoodsReceiptController(
        ToyStoreContext context,
        ISessionService sessionService,
        ILogger<GoodsReceiptController> logger)
    {
        _context = context;
        _sessionService = sessionService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var receipts = await _context.GoodsReceipts
            .AsNoTracking()
            .Include(r => r.Admin)
            .Include(r => r.GoodsReceiptDetails)
            .OrderByDescending(r => r.ImportDate)
            .ThenByDescending(r => r.ReceiptId)
            .ToListAsync();

        return View(receipts);
    }

    public async Task<IActionResult> Create()
    {
        await PopulateProductsAsync();
        return View(new CreateGoodsReceiptViewModel
        {
            Details = new List<GoodsReceiptLineViewModel>
            {
                new()
            }
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateGoodsReceiptViewModel model)
    {
        var adminId = _sessionService.GetUserId(HttpContext);
        if (adminId <= 0)
        {
            TempData["ErrorMessage"] = "Không xác định được tài khoản Admin.";
            return RedirectToAction(nameof(Create));
        }

        model.Details = (model.Details ?? new List<GoodsReceiptLineViewModel>())
            .Where(d => d.ProductId > 0 && d.Quantity > 0)
            .ToList();

        if (model.Details.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "Vui lòng thêm ít nhất một dòng sản phẩm hợp lệ.");
        }

        var productIds = model.Details.Select(d => d.ProductId).Distinct().ToList();
        var products = await _context.Products
            .Where(p => productIds.Contains(p.ProductId))
            .ToDictionaryAsync(p => p.ProductId);

        if (products.Count != productIds.Count)
        {
            ModelState.AddModelError(string.Empty, "Một hoặc nhiều sản phẩm không tồn tại.");
        }

        foreach (var line in model.Details)
        {
            if (line.ImportPrice <= 0)
            {
                ModelState.AddModelError(string.Empty, "Giá nhập phải lớn hơn 0.");
                break;
            }
        }

        if (!ModelState.IsValid)
        {
            await PopulateProductsAsync();
            if (model.Details.Count == 0)
            {
                model.Details.Add(new GoodsReceiptLineViewModel());
            }

            return View(model);
        }

        var totalCost = model.Details.Sum(d => d.Quantity * d.ImportPrice);
        var importDate = model.ImportDate.Date;

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var receipt = new GoodsReceipt
            {
                AdminId = adminId,
                ImportDate = importDate,
                TotalCost = totalCost,
                Note = model.Note
            };

            foreach (var line in model.Details)
            {
                receipt.GoodsReceiptDetails.Add(new GoodsReceiptDetail
                {
                    ProductId = line.ProductId,
                    Quantity = line.Quantity,
                    ImportPrice = line.ImportPrice
                });

                if (products.TryGetValue(line.ProductId, out var product))
                {
                    product.Stock += line.Quantity;
                }
            }

            _context.GoodsReceipts.Add(receipt);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["SuccessMessage"] =
                $"Tạo phiếu nhập kho #{receipt.ReceiptId} thành công. Tổng chi phí: {totalCost:N0} ₫";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Lỗi tạo phiếu nhập kho AdminId={AdminId}", adminId);
            ModelState.AddModelError(string.Empty, "Không thể lưu phiếu nhập kho. Vui lòng thử lại.");
            await PopulateProductsAsync();
            return View(model);
        }
    }

    private async Task PopulateProductsAsync()
    {
        var products = await _context.Products
            .AsNoTracking()
            .Where(p => p.Status == true)
            .OrderBy(p => p.ProductName)
            .Select(p => new { p.ProductId, p.ProductName, p.Stock })
            .ToListAsync();

        ViewBag.Products = new SelectList(
            products,
            "ProductId",
            "ProductName");
        ViewBag.ProductStockMap = products.ToDictionary(p => p.ProductId, p => p.Stock);
    }
}
