using ToyStore.Domain.Interfaces;

namespace ToyStore.Middleware
{
    /// <summary>
    /// Middleware để load UserSession vào HttpContext.Items
    /// Tuân thủ Dependency Injection và SOLID principles
    /// </summary>
    public class SessionMiddleware
    {
        private readonly RequestDelegate _next;

        public SessionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ISessionService sessionService)
        {
            // Resolve ISessionService từ HttpContext.RequestServices (scoped)
            // Đây là cách đúng để sử dụng scoped services trong middleware
            var userSession = sessionService.GetUserSession(context);

            if (userSession != null)
            {
                // Thêm user session vào HttpContext để sử dụng trong controllers
                context.Items["UserSession"] = userSession;
            }

            await _next(context);
        }
    }

    public static class SessionMiddlewareExtensions
    {
        public static IApplicationBuilder UseSessionMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<SessionMiddleware>();
        }
    }
}





