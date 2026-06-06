using Microsoft.EntityFrameworkCore;
using ToyStore.Models;
using ToyStore.Services;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Scripts
{
    public static class TestAuthSystem
    {
        public static async Task TestAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ToyStoreContext>();
            var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();

            Console.WriteLine("=== Testing Authentication System ===");

            // Test 1: Kiểm tra admin mặc định
            Console.WriteLine("\n1. Testing default admin login...");
            var adminLogin = await authService.LoginAsync("admin", "admin123", "Admin");
            if (adminLogin != null)
            {
                Console.WriteLine($"✓ Admin login successful: {adminLogin.FullName} ({adminLogin.Role})");
            }
            else
            {
                Console.WriteLine("✗ Admin login failed");
            }

            // Test 2: Tạo customer test
            Console.WriteLine("\n2. Testing customer registration...");
            var customerModel = new RegisterViewModel
            {
                FullName = "Nguyễn Văn A",
                Email = "test@example.com",
                Password = "123456",
                ConfirmPassword = "123456",
                Phone = "0123456789",
                Address = "123 Test Street",
                UserType = "Customer"
            };

            var customerResult = await authService.RegisterAsync(customerModel);
            if (customerResult)
            {
                Console.WriteLine("✓ Customer registration successful");
                
                // Test customer login
                var customerLogin = await authService.LoginAsync("test@example.com", "123456", "Customer");
                if (customerLogin != null)
                {
                    Console.WriteLine($"✓ Customer login successful: {customerLogin.FullName}");
                }
                else
                {
                    Console.WriteLine("✗ Customer login failed");
                }
            }
            else
            {
                Console.WriteLine("✗ Customer registration failed");
            }

            // Test 3: Tạo staff test
            Console.WriteLine("\n3. Testing staff registration...");
            var staffModel = new RegisterViewModel
            {
                FullName = "Trần Thị B",
                Email = "staff@example.com",
                Password = "123456",
                ConfirmPassword = "123456",
                Phone = "0987654321",
                Address = "456 Staff Street",
                UserType = "Staff"
            };

            var staffResult = await authService.RegisterAsync(staffModel);
            if (staffResult)
            {
                Console.WriteLine("✓ Staff registration successful");
                
                // Test staff login
                var staffLogin = await authService.LoginAsync("staff@example.com", "123456", "Staff");
                if (staffLogin != null)
                {
                    Console.WriteLine($"✓ Staff login successful: {staffLogin.FullName} ({staffLogin.Role})");
                }
                else
                {
                    Console.WriteLine("✗ Staff login failed");
                }
            }
            else
            {
                Console.WriteLine("✗ Staff registration failed");
            }

            // Test 4: Kiểm tra email trùng lặp
            Console.WriteLine("\n4. Testing duplicate email check...");
            var duplicateCheck = await authService.IsEmailExistsAsync("test@example.com");
            if (duplicateCheck)
            {
                Console.WriteLine("✓ Duplicate email check working");
            }
            else
            {
                Console.WriteLine("✗ Duplicate email check failed");
            }

            // Test 5: Test customer profile update
            Console.WriteLine("\n5. Testing customer profile update...");
            var customer = await context.Customers.FirstOrDefaultAsync(c => c.Email == "test@example.com");
            if (customer != null)
            {
                var originalName = customer.FullName;
                var originalPhone = customer.Phone;
                
                // Simulate profile update
                customer.FullName = "Nguyễn Văn A (Updated)";
                customer.Phone = "0999888777";
                customer.Address = "456 Updated Street";
                
                context.Update(customer);
                await context.SaveChangesAsync();
                
                // Verify update
                var updatedCustomer = await context.Customers.FindAsync(customer.CustomerId);
                if (updatedCustomer != null && 
                    updatedCustomer.FullName == "Nguyễn Văn A (Updated)" &&
                    updatedCustomer.Phone == "0999888777" &&
                    updatedCustomer.Address == "456 Updated Street")
                {
                    Console.WriteLine("✓ Customer profile update successful");
                }
                else
                {
                    Console.WriteLine("✗ Customer profile update failed");
                }
            }
            else
            {
                Console.WriteLine("✗ Customer not found for profile update test");
            }

            Console.WriteLine("\n=== Authentication System Test Complete ===");
        }
    }
}





