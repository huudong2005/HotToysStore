using System.Linq.Expressions;
using ToyStore.Domain.Entities;

namespace ToyStore.Domain.Interfaces;

public interface IGenericRepository<T> where T : class
{
    // Basic CRUD operations
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);
    
    // Add operations
    Task AddAsync(T entity);
    Task AddRangeAsync(IEnumerable<T> entities);
    
    // Update operations
    void Update(T entity);
    void UpdateRange(IEnumerable<T> entities);
    
    // Delete operations
    void Remove(T entity);
    void RemoveRange(IEnumerable<T> entities);
    
    // Query operations
    IQueryable<T> Query();
}
