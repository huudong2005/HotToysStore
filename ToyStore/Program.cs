using Microsoft.EntityFrameworkCore;
using Oracle.EntityFrameworkCore.Infrastructure;
using ToyStore.Application.Interfaces;
using ToyStore.Domain.Events;
using ToyStore.Domain.Factories;
using ToyStore.Domain.Interfaces;
using ToyStore.Infrastructure.Data;
using ToyStore.Infrastructure.Events;
using ToyStore.Infrastructure.Facades;
using ToyStore.Infrastructure.Payments;
using ToyStore.Infrastructure.Repositories;
using ToyStore.Infrastructure.Services;
using ToyStore.Infrastructure.UnitOfWork;
using ToyStore.Middleware;
using ToyStore.Scripts;
using ToyStore.Models;
using ToyStore.Hubs;
using ToyStore.Services;

namespace ToyStore
{
    public class Program
    {
        private static async Task EnsureOrderDiscountColumnsAsync(ToyStoreContext dbContext, ILogger logger)
        {
            // Cập nhật điều kiện kiểm tra
            if (!dbContext.Database.IsOracle()) // Đổi từ IsSqlServer sang IsOracle
            {
                logger.LogWarning("EnsureOrderDiscountColumnsAsync skipped: provider is not Oracle.");
                return;
            }

            // Vì bạn đã chạy script ToyStore_Oracle.sql thủ công, 
            // database đã chuẩn rồi, nên có thể return luôn tại đây 
            // để tránh chạy các lệnh SQL Server bên dưới gây lỗi.
            return;
        }

        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Kết nối Oracle
            // QUAN TRỌNG: Oracle.EntityFrameworkCore 8.23.x mặc định nhắm tới DB 23c (hỗ trợ BOOLEAN gốc),
            // sinh ra literal TRUE/FALSE -> gây lỗi ORA-00904 trên DB 19c/21c (khi dùng bool, .Any(), ...).
            // Khai báo DatabaseVersion19 để provider map bool -> NUMBER(1) và sinh 1/0 thay vì TRUE/FALSE.
            builder.Services.AddDbContext<ToyStoreContext>(options =>
                options.UseOracle(
                    builder.Configuration.GetConnectionString("ToyStoreDB"),
                    oracleOptions => oracleOptions.UseOracleSQLCompatibility(OracleSQLCompatibility.DatabaseVersion19)));

            // Đăng ký Unit of Work (Scoped - mỗi request một instance)
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

            // Thêm session
            builder.Services.AddDistributedMemoryCache();
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            // Đăng ký DI cho services
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<ISessionService, SessionService>();
            builder.Services.AddScoped<DiscountService>();
            builder.Services.AddScoped<IGuestCheckoutService, GuestCheckoutService>();
            builder.Services.AddScoped<ICustomerCartPersistenceService, CustomerCartPersistenceService>();
            builder.Services.AddScoped<ICartStorageService, CartStorageService>();
            builder.Services.AddScoped<DataInitializationService>();
            builder.Services.AddScoped<OrderNotificationHandler>();
            builder.Services.AddScoped<IProductRepository, ProductRepository>();
            builder.Services.AddScoped<IProductService, ProductService>();
            builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
            builder.Services.AddScoped<IAdminRepository, AdminRepository>();
            builder.Services.AddScoped<IPromotionRepository, PromotionRepository>();
            builder.Services.AddScoped<IStatisticsRepository, StatisticsRepository>();

            // Đăng ký Factories
            builder.Services.AddScoped<IUserFactory, UserFactory>();
            
            // Đăng ký Facades
            builder.Services.AddScoped<ICheckoutFacade, CheckoutFacade>();

            // Cấu hình VNPAY Sandbox
            builder.Services.Configure<VnPaySettings>(builder.Configuration.GetSection("VnPay"));
            builder.Services.Configure<GhnSettings>(builder.Configuration.GetSection("Ghn"));

            builder.Services.AddHttpClient<IGhnService, GhnService>((sp, client) =>
            {
                var settings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<GhnSettings>>().Value;
                var baseUrl = string.IsNullOrWhiteSpace(settings.BaseUrl)
                    ? "https://dev-online-gateway.ghn.vn"
                    : settings.BaseUrl.TrimEnd('/');
                client.BaseAddress = new Uri(baseUrl + "/");
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            builder.Services.AddScoped<GhnOrderShippingService>();
            builder.Services.AddScoped<IRecommendationService, RecommendationService>();

            // Đăng ký Adapter cho cổng thanh toán (Payment Gateway)
            builder.Services.AddScoped<IPaymentGateway, MockPaymentGatewayAdapter>();

            // Đăng ký Order Events (Observer / Domain Events Pattern)
            builder.Services.AddScoped<IOrderEventDispatcher, OrderEventDispatcher>();
            builder.Services.AddScoped<IOrderEventHandler<OrderConfirmedEvent>, OrderNotificationHandler>();
            builder.Services.AddScoped<IOrderEventHandler<OrderShippedEvent>, OrderNotificationHandler>();
            builder.Services.AddScoped<IOrderEventHandler<OrderCancelledEvent>, OrderNotificationHandler>();

            builder.Services.AddControllersWithViews();
            builder.Services.AddRazorPages();
            builder.Services.AddSignalR();

            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            // Thêm session middleware
            app.UseSession();
            app.UseSessionMiddleware();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.MapHub<SupportHub>("/supporthub");

            app.MapRazorPages();

            // Development: đảm bảo schema DB khớp model (tránh lỗi Invalid column name khi checkout).
            if (app.Environment.IsDevelopment())
            {
                try
                {
                    using var scope = app.Services.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<ToyStoreContext>();
                    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

                    // Try migrate first (nếu DB chưa có migration).
                    try
                    {
                        await dbContext.Database.MigrateAsync();
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Database migration skipped/failed; will ensure missing columns manually.");
                    }

                    await EnsureOrderDiscountColumnsAsync(dbContext, logger);
                }
                catch (Exception ex)
                {
                    // Không chặn chạy app, nhưng sẽ log để bạn kiểm tra connection DB.
                    var logger = app.Services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "Failed to ensure Orders discount columns.");
                }
            }

            // Khởi tạo dữ liệu mặc định (admin account)
            try
            {
                using var scope = app.Services.CreateScope();
                var dataInitService = scope.ServiceProvider.GetRequiredService<DataInitializationService>();
                await dataInitService.InitializeDefaultAdminAsync();
            }
            catch (Exception ex)
            {
                var logger = app.Services.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "Error initializing default data");
            }

            // Chạy test hệ thống (chỉ trong development)
            try
            {
                if (app.Environment.IsDevelopment())
                {
                    await TestAuthSystem.TestAsync(app.Services);
                }
            }
            catch (Exception ex)
            {
                var logger = app.Services.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "Error running dev tests");
            }

            app.Run();
        }
    }
}
