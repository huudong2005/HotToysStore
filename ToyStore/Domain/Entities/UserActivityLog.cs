namespace ToyStore.Domain.Entities;

public partial class UserActivityLog
{
    public int LogId { get; set; }

    public int CustomerId { get; set; }

    public string ActivityType { get; set; } = null!;

    public int? ProductId { get; set; }

    public string? Details { get; set; }

    public DateTime Timestamp { get; set; }

    public virtual Customer Customer { get; set; } = null!;

    public virtual Product? Product { get; set; }
}
