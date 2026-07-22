using System.Globalization;

namespace ToyStore.Helpers;

/// <summary>
/// Lưu danh sách CartItemId được chọn để thanh toán (session).
/// </summary>
public static class CartSelectionHelper
{
    public const string SelectedCartItemIdsKey = "SelectedCartItemIds";

    public static void SaveSelectedIds(HttpContext context, IEnumerable<int> cartItemIds)
    {
        var ids = cartItemIds?.Distinct().ToList() ?? new List<int>();
        context.Session.SetString(SelectedCartItemIdsKey, string.Join(",", ids));
    }

    public static List<int> GetSelectedIds(HttpContext context)
    {
        var raw = context.Session.GetString(SelectedCartItemIdsKey);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return new List<int>();
        }

        return raw
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) ? id : 0)
            .Where(id => id > 0)
            .Distinct()
            .ToList();
    }

    public static void ClearSelectedIds(HttpContext context)
    {
        context.Session.Remove(SelectedCartItemIdsKey);
    }
}
