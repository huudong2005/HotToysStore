using System.Text.Json;
using ToyStore.Models;

namespace ToyStore.Helpers;

/// <summary>
/// Lưu thông tin checkout và quyền xem đơn của khách vãng lai trong session.
/// </summary>
public static class GuestOrderSession
{
    public const string GuestCheckoutKey = "GuestCheckoutInfo";
    public const string AccessibleOrdersKey = "GuestAccessibleOrderIds";

    public static void SaveGuestCheckout(HttpContext context, GuestCheckoutInfo info)
    {
        context.Session.SetString(GuestCheckoutKey, JsonSerializer.Serialize(info));
    }

    public static GuestCheckoutInfo? GetGuestCheckout(HttpContext context)
    {
        var json = context.Session.GetString(GuestCheckoutKey);
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<GuestCheckoutInfo>(json);
    }

    public static void ClearGuestCheckout(HttpContext context)
    {
        context.Session.Remove(GuestCheckoutKey);
    }

    public static void GrantOrderAccess(HttpContext context, int orderId)
    {
        var ids = GetAccessibleOrderIds(context);
        if (!ids.Contains(orderId))
        {
            ids.Add(orderId);
        }

        context.Session.SetString(AccessibleOrdersKey, string.Join(",", ids));
    }

    public static bool CanAccessOrder(HttpContext context, int orderId, int loggedInCustomerId)
    {
        if (loggedInCustomerId > 0)
        {
            return true;
        }

        return GetAccessibleOrderIds(context).Contains(orderId);
    }

    private static List<int> GetAccessibleOrderIds(HttpContext context)
    {
        var raw = context.Session.GetString(AccessibleOrdersKey);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return new List<int>();
        }

        return raw.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => int.TryParse(s.Trim(), out var id) ? id : 0)
            .Where(id => id > 0)
            .Distinct()
            .ToList();
    }
}
