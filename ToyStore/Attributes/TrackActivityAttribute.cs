using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ToyStore.Domain.Entities;
using ToyStore.Domain.Interfaces;
using ToyStore.Infrastructure.Data;

namespace ToyStore.Attributes;

/// <summary>
/// Ghi nhận hành vi xem sản phẩm / tìm kiếm của khách hàng đã đăng nhập.
/// Lỗi ghi log không được làm gián đoạn request của người dùng.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class TrackActivityAttribute : ActionFilterAttribute, IAsyncActionFilter
{
    private const int MaxDetailsLength = 500;

    public override async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        await TryTrackActivityAsync(context);
        await next();
    }

    private static async Task TryTrackActivityAsync(ActionExecutingContext context)
    {
        try
        {
            var httpContext = context.HttpContext;
            var sessionService = httpContext.RequestServices.GetService<ISessionService>();

            if (sessionService == null || !sessionService.IsCustomer(httpContext))
            {
                return;
            }

            var customerId = sessionService.GetUserId(httpContext);
            if (customerId <= 0)
            {
                return;
            }

            if (!TryResolveActivity(context, out var activityType, out var productId, out var details))
            {
                return;
            }

            await using var scope = httpContext.RequestServices.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ToyStoreContext>();

            dbContext.UserActivityLogs.Add(new UserActivityLog
            {
                CustomerId = customerId,
                ActivityType = activityType,
                ProductId = productId,
                Details = details,
                Timestamp = DateTime.Now
            });

            await dbContext.SaveChangesAsync(httpContext.RequestAborted);
        }
        catch (OperationCanceledException)
        {
            // Request bị hủy — bỏ qua, không ghi log.
        }
        catch (Exception ex)
        {
            var logger = context.HttpContext.RequestServices.GetService<ILogger<TrackActivityAttribute>>();
            logger?.LogWarning(ex, "Không thể ghi UserActivityLog cho {Controller}/{Action}",
                context.RouteData.Values["controller"],
                context.RouteData.Values["action"]);
        }
    }

    private static bool TryResolveActivity(
        ActionExecutingContext context,
        out string activityType,
        out int? productId,
        out string? details)
    {
        activityType = string.Empty;
        productId = null;
        details = null;

        var controller = context.RouteData.Values["controller"]?.ToString();
        var action = context.RouteData.Values["action"]?.ToString();

        if (IsViewProductAction(controller, action))
        {
            if (!TryGetProductId(context, out var resolvedProductId))
            {
                return false;
            }

            activityType = "ViewProduct";
            productId = resolvedProductId;
            return true;
        }

        if (IsSearchAction(controller, action))
        {
            var searchTerm = GetSearchTerm(context);
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return false;
            }

            activityType = "Search";
            details = TruncateDetails(searchTerm.Trim());
            return true;
        }

        return false;
    }

    private static bool IsViewProductAction(string? controller, string? action)
    {
        return (string.Equals(controller, "Home", StringComparison.OrdinalIgnoreCase)
                && string.Equals(action, "ProductDetails", StringComparison.OrdinalIgnoreCase))
               || (string.Equals(controller, "Products", StringComparison.OrdinalIgnoreCase)
                   && string.Equals(action, "Details", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsSearchAction(string? controller, string? action)
    {
        if (!string.Equals(controller, "Home", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return string.Equals(action, "Index", StringComparison.OrdinalIgnoreCase)
               || string.Equals(action, "Shop", StringComparison.OrdinalIgnoreCase)
               || string.Equals(action, "SearchSuggestions", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryGetProductId(ActionExecutingContext context, out int productId)
    {
        productId = 0;

        if (context.ActionArguments.TryGetValue("id", out var idValue))
        {
            if (idValue is int intId && intId > 0)
            {
                productId = intId;
                return true;
            }

            if (int.TryParse(idValue?.ToString(), out var parsedId) && parsedId > 0)
            {
                productId = parsedId;
                return true;
            }
        }

        if (context.RouteData.Values.TryGetValue("id", out var routeId)
            && int.TryParse(routeId?.ToString(), out var routeParsedId)
            && routeParsedId > 0)
        {
            productId = routeParsedId;
            return true;
        }

        return false;
    }

    private static string? GetSearchTerm(ActionExecutingContext context)
    {
        foreach (var key in new[] { "searchName", "keyword", "query", "searchTerm" })
        {
            if (!context.ActionArguments.TryGetValue(key, out var value) || value == null)
            {
                continue;
            }

            var text = value as string ?? value.ToString();
            if (!string.IsNullOrWhiteSpace(text))
            {
                return text;
            }
        }

        return null;
    }

    private static string TruncateDetails(string value)
    {
        return value.Length <= MaxDetailsLength
            ? value
            : value[..MaxDetailsLength];
    }
}
