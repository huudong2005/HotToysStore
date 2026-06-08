using ToyStore.Domain.Entities;

namespace ToyStore.Domain.Interfaces;

public interface ICustomerRepository : IGenericRepository<Customer>
{
    Task<Customer?> GetCustomerByEmailAsync(string email);
    Task<bool> EmailExistsAsync(string email);

    /// <summary>
    /// Lấy khách hàng kèm thông tin hạng thẻ (Include Tier) để biết DiscountPercent.
    /// </summary>
    Task<Customer?> GetCustomerWithTierAsync(int customerId);
    Task CreateCustomerViaProcedureAsync(Customer customer);
    Task UpdateCustomerViaProcedureAsync(Customer customer, string? passwordHash);
    Task<int> DeleteCustomerViaProcedureAsync(int customerId);
}
