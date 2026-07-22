using ToyStore.Domain.Entities;

namespace ToyStore.Models;

public class ActivityLogIndexViewModel
{
    public List<UserActivityLog> Logs { get; set; } = new();

    public int CurrentPage { get; set; }

    public int TotalPages { get; set; }

    public int TotalCount { get; set; }

    public int PageSize { get; set; } = 20;

    public string? SearchEmail { get; set; }

    public string? ActivityType { get; set; }

    public bool HasFilters =>
        !string.IsNullOrWhiteSpace(SearchEmail) || !string.IsNullOrWhiteSpace(ActivityType);
}
