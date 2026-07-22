using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ToyStore.Domain.Entities;

[Table("GoodsReceipt")]
public partial class GoodsReceipt
{
    [Key]
    [Column("ReceiptId")]
    public int ReceiptId { get; set; }

    [Column("AdminId")]
    public int AdminId { get; set; }

    [Column("ImportDate")]
    public DateTime ImportDate { get; set; }

    [Column("TotalCost")]
    public decimal TotalCost { get; set; }

    [Column("Note")]
    public string? Note { get; set; }

    public virtual Admin Admin { get; set; } = null!;

    public virtual ICollection<GoodsReceiptDetail> GoodsReceiptDetails { get; set; } = new List<GoodsReceiptDetail>();
}
