using Microsoft.EntityFrameworkCore;
using ToyStore.Domain.Entities;
using ToyStore.Domain.Interfaces;
using ToyStore.Infrastructure.Data;
using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace ToyStore.Infrastructure.Repositories;

public class CustomerRepository : GenericRepository<Customer>, ICustomerRepository
{
    public CustomerRepository(ToyStoreContext context) : base(context)
    {
    }

    public async Task<Customer?> GetCustomerByEmailAsync(string email)
    {
        return await _dbSet
            .FirstOrDefaultAsync(c => c.Email == email);
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        return await _dbSet
            .CountAsync(c => c.Email == email) > 0;
    }

    public async Task<Customer?> GetCustomerWithTierAsync(int customerId)
    {
        return await _dbSet
            .Include(c => c.Tier)
            .FirstOrDefaultAsync(c => c.CustomerId == customerId);
    }

    public async Task CreateCustomerViaProcedureAsync(Customer customer)
    {
        var p_FullName = new OracleParameter("p_FullName", OracleDbType.Varchar2, 100)
        {
            Value = customer.FullName
        };
        var p_Email = new OracleParameter("p_Email", OracleDbType.Varchar2, 100)
        {
            Value = customer.Email
        };
        var p_Phone = new OracleParameter("p_Phone", OracleDbType.Varchar2, 20)
        {
            Value = (object?)customer.Phone ?? DBNull.Value
        };
        var p_Address = new OracleParameter("p_Address", OracleDbType.Varchar2, 255)
        {
            Value = (object?)customer.Address ?? DBNull.Value
        };
        var p_PasswordHash = new OracleParameter("p_PasswordHash", OracleDbType.Varchar2, 255)
        {
            Value = customer.PasswordHash
        };

        string sql = "BEGIN \"SP_CreateCustomer\"(:p_FullName, :p_Email, :p_Phone, :p_Address, :p_PasswordHash); END;";
        await _context.Database.ExecuteSqlRawAsync(sql, p_FullName, p_Email, p_Phone, p_Address, p_PasswordHash);
    }

    public async Task UpdateCustomerViaProcedureAsync(Customer customer, string? passwordHash)
    {
        var p_CustomerId = new OracleParameter("p_CustomerId", OracleDbType.Int32) { Value = customer.CustomerId };
        var p_FullName = new OracleParameter("p_FullName", OracleDbType.Varchar2, 100) { Value = customer.FullName };
        var p_Email = new OracleParameter("p_Email", OracleDbType.Varchar2, 100) { Value = customer.Email };
        var p_Phone = new OracleParameter("p_Phone", OracleDbType.Varchar2, 20)
        {
            Value = (object?)customer.Phone ?? DBNull.Value
        };
        var p_Address = new OracleParameter("p_Address", OracleDbType.Varchar2, 255)
        {
            Value = (object?)customer.Address ?? DBNull.Value
        };
        var p_PasswordHash = new OracleParameter("p_PasswordHash", OracleDbType.Varchar2, 255)
        {
            Value = string.IsNullOrWhiteSpace(passwordHash) ? DBNull.Value : passwordHash
        };

        string sql = "BEGIN \"SP_UpdateCustomer\"(:p_CustomerId, :p_FullName, :p_Email, :p_Phone, :p_Address, :p_PasswordHash); END;";
        await _context.Database.ExecuteSqlRawAsync(sql, p_CustomerId, p_FullName, p_Email, p_Phone, p_Address, p_PasswordHash);
    }

    public async Task<int> DeleteCustomerViaProcedureAsync(int customerId)
    {
        var p_CustomerId = new OracleParameter("p_CustomerId", OracleDbType.Int32) { Value = customerId };
        var p_ResultCode = new OracleParameter("p_ResultCode", OracleDbType.Int32)
        {
            Direction = ParameterDirection.Output
        };

        string sql = "BEGIN \"SP_DeleteCustomer\"(:p_CustomerId, :p_ResultCode); END;";
        await _context.Database.ExecuteSqlRawAsync(sql, p_CustomerId, p_ResultCode);

        return Convert.ToInt32(p_ResultCode.Value.ToString());
    }
}
