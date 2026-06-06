using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ToyStore.Models;
using ToyStore.Domain.Entities;
using ToyStore.Infrastructure.Data;

namespace ToyStore.ViewComponents;

public class CategoryListViewComponent : ViewComponent
{
    private readonly ToyStoreContext _context;

    public CategoryListViewComponent(ToyStoreContext context)
    {
        _context = context;
    }

    public async Task<IViewComponentResult> InvokeAsync(int? take = null)
    {
        var query = _context.Categories
            .Include(c => c.Products)
            .OrderByDescending(c => c.Products.Count)
            .ThenBy(c => c.CategoryName)
            .AsQueryable();

        if (take.HasValue && take.Value > 0)
        {
            query = query.Take(take.Value);
        }

        var categories = await query.ToListAsync();
        return View(categories);
    }
}

