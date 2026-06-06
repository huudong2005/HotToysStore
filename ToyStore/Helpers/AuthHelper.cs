using ToyStore.Models;

namespace ToyStore.Helpers
{
    /// <summary>
    /// Helper class để tương thích ngược (backward compatibility)
    /// Nên sử dụng ISessionService thay vì AuthHelper trong code mới
    /// </summary>
    public static class AuthHelper
    {
        /// <summary>
        /// Lấy thông tin user từ HttpContext.Items (được set bởi SessionMiddleware)
        /// </summary>
        public static UserSession? GetCurrentUser(HttpContext context)
        {
            return context.Items["UserSession"] as UserSession;
        }

        /// <summary>
        /// Kiểm tra user đã đăng nhập chưa (backward compatibility)
        /// </summary>
        public static bool IsAuthenticated(HttpContext context)
        {
            return context.Session.GetString("IsAuthenticated") == "True";
        }

        /// <summary>
        /// Kiểm tra user có phải Admin không (backward compatibility)
        /// </summary>
        public static bool IsAdmin(HttpContext context)
        {
            return IsAuthenticated(context) && context.Session.GetString("UserType") == "Admin";
        }

        /// <summary>
        /// Kiểm tra user có phải Staff hoặc Admin không (backward compatibility)
        /// </summary>
        public static bool IsStaff(HttpContext context)
        {
            return IsAuthenticated(context) && (context.Session.GetString("UserType") == "Staff" || context.Session.GetString("UserType") == "Admin");
        }

        /// <summary>
        /// Kiểm tra user có phải Customer không (backward compatibility)
        /// </summary>
        public static bool IsCustomer(HttpContext context)
        {
            return IsAuthenticated(context) && context.Session.GetString("UserType") == "Customer";
        }

        /// <summary>
        /// Kiểm tra user có Role cụ thể không (backward compatibility)
        /// </summary>
        public static bool HasRole(HttpContext context, string role)
        {
            return IsAuthenticated(context) && context.Session.GetString("Role") == role;
        }

        /// <summary>
        /// Kiểm tra user có bất kỳ Role nào trong danh sách không (backward compatibility)
        /// </summary>
        public static bool HasAnyRole(HttpContext context, params string[] roles)
        {
            if (!IsAuthenticated(context)) return false;
            
            var userRole = context.Session.GetString("Role");
            return roles.Contains(userRole);
        }
    }
}





