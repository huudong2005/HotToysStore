using ToyStore.Domain.Interfaces;
using ToyStore.Helpers;
using ToyStore.Models;

namespace ToyStore.Services;

public class CartStorageService : ICartStorageService
{
    public const string CartSessionKey = "ShoppingCart";

    private readonly ISessionService _sessionService;
    private readonly ICustomerCartPersistenceService _cartPersistence;

    public CartStorageService(
        ISessionService sessionService,
        ICustomerCartPersistenceService cartPersistence)
    {
        _sessionService = sessionService;
        _cartPersistence = cartPersistence;
    }

    public async Task<ShoppingCart> GetCartAsync(HttpContext context)
    {
        var sessionCart = GetSessionCart(context);

        if (sessionCart.Items.Any())
        {
            return sessionCart;
        }

        if (!_sessionService.IsCustomer(context))
        {
            return sessionCart;
        }

        var customerId = _sessionService.GetUserId(context);
        if (customerId <= 0)
        {
            return sessionCart;
        }

        var dbCart = await _cartPersistence.LoadAsync(customerId);
        if (dbCart.Items.Any())
        {
            SetSessionCart(context, dbCart);
        }

        return dbCart;
    }

    public async Task SaveCartAsync(HttpContext context, ShoppingCart cart)
    {
        SetSessionCart(context, cart);

        if (_sessionService.IsCustomer(context))
        {
            var customerId = _sessionService.GetUserId(context);
            if (customerId > 0)
            {
                await _cartPersistence.SaveAsync(customerId, cart);
            }
        }
    }

    public async Task RestoreCartAfterLoginAsync(HttpContext context, int customerId)
    {
        var sessionCart = GetSessionCart(context);
        var dbCart = await _cartPersistence.LoadAsync(customerId);
        var merged = MergeCarts(dbCart, sessionCart);

        SetSessionCart(context, merged);
        await _cartPersistence.SaveAsync(customerId, merged);
    }

    public async Task PersistCartBeforeLogoutAsync(HttpContext context, int customerId)
    {
        var sessionCart = GetSessionCart(context);
        await _cartPersistence.SaveAsync(customerId, sessionCart);
    }

    public async Task ClearCartAfterOrderAsync(HttpContext context, int customerId)
    {
        SetSessionCart(context, new ShoppingCart());
        CartSelectionHelper.ClearSelectedIds(context);

        if (customerId > 0)
        {
            await _cartPersistence.ClearAsync(customerId);
        }
    }

    public async Task RemoveItemsAsync(HttpContext context, IEnumerable<int> cartItemIds, int customerId)
    {
        var cart = await GetCartAsync(context);
        cart.EnsureCartItemIds();

        var idSet = cartItemIds.ToHashSet();
        cart.Items.RemoveAll(i => idSet.Contains(i.CartItemId));

        CartSelectionHelper.ClearSelectedIds(context);

        if (!cart.Items.Any())
        {
            await ClearCartAfterOrderAsync(context, customerId);
            return;
        }

        await SaveCartAsync(context, cart);
    }

    private static ShoppingCart GetSessionCart(HttpContext context)
    {
        var cartJson = context.Session.GetString(CartSessionKey);
        return ShoppingCart.FromJson(cartJson ?? string.Empty);
    }

    private static void SetSessionCart(HttpContext context, ShoppingCart cart)
    {
        context.Session.SetString(CartSessionKey, cart.ToJson());
    }

    /// <summary>
    /// Gộp giỏ DB và giỏ session (cùng sản phẩm thì cộng số lượng).
    /// </summary>
    private static ShoppingCart MergeCarts(ShoppingCart primary, ShoppingCart secondary)
    {
        var merged = new ShoppingCart
        {
            DiscountStrategyName = secondary.DiscountStrategyName ?? primary.DiscountStrategyName
        };

        foreach (var item in primary.Items)
        {
            merged.Items.Add(new ShoppingCartItem
            {
                CartItemId = item.CartItemId != 0 ? item.CartItemId : item.ProductId,
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                Price = item.Price,
                Quantity = item.Quantity,
                ImageUrl = item.ImageUrl
            });
        }

        foreach (var item in secondary.Items)
        {
            var existing = merged.Items.FirstOrDefault(i => i.ProductId == item.ProductId);
            if (existing != null)
            {
                existing.Quantity += item.Quantity;
            }
            else
            {
                merged.Items.Add(new ShoppingCartItem
                {
                    CartItemId = item.CartItemId != 0 ? item.CartItemId : item.ProductId,
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    Price = item.Price,
                    Quantity = item.Quantity,
                    ImageUrl = item.ImageUrl
                });
            }
        }

        return merged;
    }
}
