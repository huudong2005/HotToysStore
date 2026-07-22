using Microsoft.EntityFrameworkCore;
using ToyStore.Infrastructure.Data;
using ToyStore.Models;

namespace ToyStore.Services;

public class CustomerCartPersistenceService : ICustomerCartPersistenceService
{
    private readonly ToyStoreContext _context;

    public CustomerCartPersistenceService(ToyStoreContext context)
    {
        _context = context;
    }

    public async Task SaveAsync(int customerId, ShoppingCart cart)
    {
        var dbCart = await _context.Carts
            .Include(c => c.CartItems)
            .Where(c => c.CustomerId == customerId)
            .OrderByDescending(c => c.CartId)
            .FirstOrDefaultAsync();

        if (dbCart == null)
        {
            dbCart = new Domain.Entities.Cart
            {
                CustomerId = customerId,
                CreatedAt = DateTime.Now,
                CartItems = new List<Domain.Entities.CartItem>()
            };
            _context.Carts.Add(dbCart);
        }
        else
        {
            _context.CartItems.RemoveRange(dbCart.CartItems);
        }

        foreach (var item in cart.Items)
        {
            dbCart.CartItems.Add(new Domain.Entities.CartItem
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity
            });
        }

        await _context.SaveChangesAsync();
    }

    public async Task<ShoppingCart> LoadAsync(int customerId)
    {
        var dbCart = await _context.Carts
            .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
            .Where(c => c.CustomerId == customerId)
            .OrderByDescending(c => c.CartId)
            .FirstOrDefaultAsync();

        var cart = new ShoppingCart();

        if (dbCart?.CartItems == null || !dbCart.CartItems.Any())
        {
            return cart;
        }

        foreach (var cartItem in dbCart.CartItems)
        {
            if (cartItem.Product == null || cartItem.Product.Status != true)
            {
                continue;
            }

            cart.Items.Add(new ShoppingCartItem
            {
                CartItemId = cartItem.CartItemId,
                ProductId = cartItem.ProductId,
                ProductName = cartItem.Product.ProductName,
                Price = cartItem.Product.Price,
                Quantity = cartItem.Quantity,
                ImageUrl = cartItem.Product.ImageUrl
            });
        }

        return cart;
    }

    public async Task ClearAsync(int customerId)
    {
        var dbCarts = await _context.Carts
            .Include(c => c.CartItems)
            .Where(c => c.CustomerId == customerId)
            .ToListAsync();

        if (!dbCarts.Any())
        {
            return;
        }

        foreach (var dbCart in dbCarts)
        {
            _context.CartItems.RemoveRange(dbCart.CartItems);
            _context.Carts.Remove(dbCart);
        }

        await _context.SaveChangesAsync();
    }
}
