using Microsoft.EntityFrameworkCore.Storage;
using ToyStore.Domain.Entities;
using ToyStore.Domain.Interfaces;
using ToyStore.Infrastructure.Data;
using ToyStore.Infrastructure.Repositories;

namespace ToyStore.Infrastructure.UnitOfWork;

public class UnitOfWork : IUnitOfWork
{
    private readonly ToyStoreContext _context;
    private IDbContextTransaction? _transaction;

    // Repositories
    private IProductRepository? _products;
    private ICategoryRepository? _categories;
    private ICustomerRepository? _customers;
    private IOrderRepository? _orders;
    private IOrderDetailRepository? _orderDetails;
    private IAdminRepository? _admins;
    private IPromotionRepository? _promotions;
    private IBannerRepository? _banners;
    private IGenericRepository<Cart>? _carts;
    private IGenericRepository<CartItem>? _cartItems;

    public UnitOfWork(ToyStoreContext context)
    {
        _context = context;
    }

    public IProductRepository Products
    {
        get
        {
            _products ??= new ProductRepository(_context);
            return _products;
        }
    }

    public ICategoryRepository Categories
    {
        get
        {
            _categories ??= new CategoryRepository(_context);
            return _categories;
        }
    }

    public ICustomerRepository Customers
    {
        get
        {
            _customers ??= new CustomerRepository(_context);
            return _customers;
        }
    }

    public IOrderRepository Orders
    {
        get
        {
            _orders ??= new OrderRepository(_context);
            return _orders;
        }
    }

    public IOrderDetailRepository OrderDetails
    {
        get
        {
            _orderDetails ??= new OrderDetailRepository(_context);
            return _orderDetails;
        }
    }

    public IAdminRepository Admins
    {
        get
        {
            _admins ??= new AdminRepository(_context);
            return _admins;
        }
    }

    public IPromotionRepository Promotions
    {
        get
        {
            _promotions ??= new PromotionRepository(_context);
            return _promotions;
        }
    }

    public IBannerRepository Banners
    {
        get
        {
            _banners ??= new BannerRepository(_context);
            return _banners;
        }
    }

    public IGenericRepository<Cart> Carts
    {
        get
        {
            _carts ??= new GenericRepository<Cart>(_context);
            return _carts;
        }
    }

    public IGenericRepository<CartItem> CartItems
    {
        get
        {
            _cartItems ??= new GenericRepository<CartItem>(_context);
            return _cartItems;
        }
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}
