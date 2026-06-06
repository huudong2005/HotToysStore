using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;
using ToyStore.Domain.Entities;
using ToyStore.Domain.Interfaces;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Infrastructure.Repositories;

public class AdminRepository : GenericRepository<Admin>, IAdminRepository
{
    public AdminRepository(ToyStoreContext context) : base(context)
    {
    }

    public async Task<Admin?> GetAdminByUsernameAsync(string username)
    {
        return await _dbSet
            .FirstOrDefaultAsync(a => a.Username == username);
    }

    public async Task<bool> UsernameExistsAsync(string username)
    {
        return await _dbSet.FirstOrDefaultAsync(a => a.Username == username) != null;
    }
    // ---- TRIỂN KHAI STORED PROCEDURE CHO ORACLE ----
    public async Task CreateStaffViaProcedureAsync(Admin admin)
    {
        var parameters = new[]
        {
            new OracleParameter("p_Username", OracleDbType.Varchar2, 50) { Value = admin.Username },
            new OracleParameter("p_PasswordHash", OracleDbType.Varchar2, 255) { Value = admin.PasswordHash },
            new OracleParameter("p_FullName", OracleDbType.Varchar2, 100) { Value = (object)admin.FullName ?? DBNull.Value },
            new OracleParameter("p_Role", OracleDbType.Varchar2, 50) { Value = admin.Role ?? "Staff" }
        };

        string sql = "BEGIN \"SP_CreateStaff\"(:p_Username, :p_PasswordHash, :p_FullName, :p_Role); END;";
        await _context.Database.ExecuteSqlRawAsync(sql, parameters);
    }

    public async Task UpdateStaffViaProcedureAsync(Admin admin)
    {
        var parameters = new[]
        {
            new OracleParameter("p_AdminId", OracleDbType.Int32) { Value = admin.AdminId },
            new OracleParameter("p_Username", OracleDbType.Varchar2, 50) { Value = admin.Username },
            new OracleParameter("p_PasswordHash", OracleDbType.Varchar2, 255) { Value = admin.PasswordHash },
            new OracleParameter("p_FullName", OracleDbType.Varchar2, 100) { Value = (object)admin.FullName ?? DBNull.Value },
            new OracleParameter("p_Role", OracleDbType.Varchar2, 50) { Value = admin.Role ?? "Staff" }
        };

        string sql = "BEGIN \"SP_UpdateStaff\"(:p_AdminId, :p_Username, :p_PasswordHash, :p_FullName, :p_Role); END;";
        await _context.Database.ExecuteSqlRawAsync(sql, parameters);
    }

    public async Task DeleteStaffViaProcedureAsync(int adminId)
    {
        var p_AdminId = new OracleParameter("p_AdminId", OracleDbType.Int32) { Value = adminId };
        string sql = "BEGIN \"SP_DeleteStaff\"(:p_AdminId); END;";
        await _context.Database.ExecuteSqlRawAsync(sql, p_AdminId);
    }
}
