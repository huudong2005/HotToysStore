using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using ToyStore.Domain.Entities;
using ToyStore.Domain.Interfaces;
using ToyStore.Infrastructure.Data;
using ToyStore.Infrastructure.Repositories;
using ToyStore.Models;

namespace ToyStore.Services
{
    public interface IAuthService
    {
        Task<UserSession?> LoginAsync(string emailOrUsername, string password, string userType);
        Task<bool> RegisterAsync(RegisterViewModel model);
        Task<bool> CreateStaffAsync(CreateStaffViewModel model);
        Task<bool> IsEmailExistsAsync(string email);
        Task<bool> IsUsernameExistsAsync(string username);
        string HashPassword(string password);
        bool VerifyPassword(string password, string hash);
    }

    public class AuthService : IAuthService
    {
        private readonly ToyStoreContext _context;
        private readonly IUserFactory _userFactory;
        private readonly IAdminRepository _adminRepository;

        // Sửa Constructor để nhận thêm IAdminRepository
        public AuthService(ToyStoreContext context, IUserFactory userFactory, IAdminRepository adminRepository)
        {
            _context = context;
            _userFactory = userFactory;
            _adminRepository = adminRepository; // Gán giá trị ở Bước 2
        }

        public async Task<UserSession?> LoginAsync(string emailOrUsername, string password, string userType)
        {
            switch (userType.ToLower())
            {
                case "customer":
                    return await LoginCustomerAsync(emailOrUsername, password);
                case "admin":
                case "staff":
                    return await LoginAdminAsync(emailOrUsername, password);
                default:
                    return null;
            }
        }

        private async Task<UserSession?> LoginCustomerAsync(string emailOrUsername, string password)
        {
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Email == emailOrUsername);

            if (customer == null)
                return null;

            // Hỗ trợ tương thích dữ liệu cũ: nếu từng lưu mật khẩu plain text thì cho đăng nhập 1 lần
            // và tự động nâng cấp thành SHA256 hash.
            if (!VerifyPassword(password, customer.PasswordHash))
            {
                if (customer.PasswordHash != password)
                {
                    return null;
                }

                customer.PasswordHash = HashPassword(password);
                await _context.SaveChangesAsync();
            }

            // Tài khoản bị khóa — không cấp phiên đăng nhập.
            if (customer.IsLocked)
            {
                return null;
            }

            return new UserSession
            {
                UserId = customer.CustomerId,
                Username = customer.Email,
                Email = customer.Email,
                FullName = customer.FullName,
                UserType = "Customer",
                Role = "Customer",
                IsAuthenticated = true
            };
        }

        private async Task<UserSession?> LoginAdminAsync(string emailOrUsername, string password)
        {
            var admin = await _context.Admins
                .FirstOrDefaultAsync(a => a.Username == emailOrUsername);

            if (admin == null || !VerifyPassword(password, admin.PasswordHash))
                return null;

            return new UserSession
            {
                UserId = admin.AdminId,
                Username = admin.Username,
                Email = admin.Username, // Admin sử dụng username thay vì email
                FullName = admin.FullName ?? admin.Username,
                UserType = admin.Role ?? "Staff",
                Role = admin.Role ?? "Staff",
                IsAuthenticated = true
            };
        }

        public async Task<bool> RegisterAsync(RegisterViewModel model)
        {
            // Chỉ cho phép đăng ký Customer
            if (model.UserType.ToLower() == "customer")
            {
                return await RegisterCustomerAsync(model);
            }
            return false;
        }

        // Sửa lại hàm này trong file AuthService.cs của bạn
        public async Task<bool> CreateStaffAsync(CreateStaffViewModel model)
        {
            if (await IsUsernameExistsAsync(model.Username))
                return false;

            var passwordHash = HashPassword(model.Password);
            Admin admin;

            if (model.Role?.ToLower() == "admin")
            {
                admin = _userFactory.CreateAdmin(model, passwordHash);
            }
            else
            {
                admin = _userFactory.CreateStaff(model, passwordHash);
            }

            // GỌI QUA REPOSITORY ĐỂ ĐẨY XUỐNG ORACLE PROCEDURE
            // (Cần tiêm private readonly IAdminRepository _adminRepository vào Constructor của AuthService nhé)
            await _adminRepository.CreateStaffViaProcedureAsync(admin);
            return true;
        }

        private async Task<bool> RegisterCustomerAsync(RegisterViewModel model)
        {
            if (await IsEmailExistsAsync(model.Email))
                return false;

            // Sử dụng UserFactory để tạo Customer với Role/Claims mặc định
            var passwordHash = HashPassword(model.Password);
            var customer = _userFactory.CreateCustomer(model, passwordHash);

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();
            return true;
        }

        private async Task<bool> RegisterStaffAsync(RegisterViewModel model)
        {
            if (await IsUsernameExistsAsync(model.Email)) // Staff sử dụng email làm username
                return false;

            // Sử dụng UserFactory để tạo Staff
            var passwordHash = HashPassword(model.Password);
            var admin = _userFactory.CreateStaff(
                new CreateStaffViewModel
                {
                    Username = model.Email,
                    FullName = model.FullName,
                    Password = model.Password,
                    ConfirmPassword = model.Password,
                    Role = "Staff"
                },
                passwordHash
            );

            _context.Admins.Add(admin);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IsEmailExistsAsync(string email)
        {
            // Thay vì dùng AnyAsync, ta dùng FirstOrDefaultAsync để tránh lỗi TRUE/FALSE của Oracle
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email == email);
            return customer != null;
        }

        public async Task<bool> IsUsernameExistsAsync(string username)
        {
            // Thay vì dùng AnyAsync, ta dùng FirstOrDefaultAsync để tránh lỗi TRUE/FALSE của Oracle
            var admin = await _context.Admins.FirstOrDefaultAsync(a => a.Username == username);
            return admin != null;
        }

        public string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        public bool VerifyPassword(string password, string hash)
        {
            var hashedPassword = HashPassword(password);
            return hashedPassword == hash;
        }
    }
}
