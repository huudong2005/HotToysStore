using ToyStore.Domain.Entities;

namespace ToyStore.Domain.Interfaces;

public interface IAdminRepository : IGenericRepository<Admin>
{
    Task<Admin?> GetAdminByUsernameAsync(string username);
    Task<bool> UsernameExistsAsync(string username);

    // Thêm 3 phương thức mới cho Oracle Stored Procedure
    Task CreateStaffViaProcedureAsync(Admin admin);
    Task UpdateStaffViaProcedureAsync(Admin admin);
    Task DeleteStaffViaProcedureAsync(int adminId);
}
