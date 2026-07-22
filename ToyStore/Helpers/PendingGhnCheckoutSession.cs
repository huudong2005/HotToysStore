using System.Text.Json;
using ToyStore.Models;

namespace ToyStore.Helpers;

public static class PendingGhnCheckoutSession
{
    private const string DraftKey = "PendingGhnCheckoutDraft";

    private static string OrderKey(int orderId) => $"PendingGhnCheckout_Order_{orderId}";

    public static void SaveDraft(HttpContext context, PendingGhnCheckoutData data)
    {
        context.Session.SetString(DraftKey, JsonSerializer.Serialize(data));
    }

    public static PendingGhnCheckoutData? GetDraft(HttpContext context)
    {
        var json = context.Session.GetString(DraftKey);
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<PendingGhnCheckoutData>(json);
    }

    public static void BindDraftToOrder(HttpContext context, int orderId)
    {
        var draft = GetDraft(context);
        if (draft == null)
        {
            return;
        }

        context.Session.SetString(OrderKey(orderId), JsonSerializer.Serialize(draft));
        context.Session.Remove(DraftKey);
    }

    public static PendingGhnCheckoutData? GetForOrder(HttpContext context, int orderId)
    {
        var json = context.Session.GetString(OrderKey(orderId));
        if (string.IsNullOrEmpty(json))
        {
            return GetDraft(context);
        }

        return JsonSerializer.Deserialize<PendingGhnCheckoutData>(json);
    }

    public static void ClearForOrder(HttpContext context, int orderId)
    {
        context.Session.Remove(OrderKey(orderId));
        context.Session.Remove(DraftKey);
    }
}
