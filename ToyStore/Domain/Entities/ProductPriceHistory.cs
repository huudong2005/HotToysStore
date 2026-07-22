using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ToyStore.Domain.Entities;

[Table("ProductPriceHistory")]
public partial class ProductPriceHistory
{
    [Key]
    [Column("HistoryId")]
    public int HistoryId { get; set; }

    [Column("ProductId")]
    public int ProductId { get; set; }

    [Column("OldPrice")]
    public decimal OldPrice { get; set; }

    [Column("NewPrice")]
    public decimal NewPrice { get; set; }

    [Column("ChangedAt")]
    public DateTime ChangedAt { get; set; }

    [Column("AdminId")]
    public int AdminId { get; set; }

    public virtual Product? Product { get; set; }

    public virtual Admin? Admin { get; set; }
}
