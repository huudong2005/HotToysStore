namespace ToyStore.Models;

public class ProfitReportViewModel
{
    public int Month { get; set; }

    public int Year { get; set; }

    public decimal TotalRevenue { get; set; }

    public decimal TotalCogs { get; set; }

    public decimal GrossProfit => TotalRevenue - TotalCogs;

    public bool IsProfit => GrossProfit >= 0;
}
