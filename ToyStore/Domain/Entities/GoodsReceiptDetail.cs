using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ToyStore.Domain.Entities;

[Table("GoodsReceiptDetail")]
public partial class GoodsReceiptDetail
{
    [Key]
    [Column("DetailId")]
    public int DetailId { get; set; }

    [Column("ReceiptId")]
    public int ReceiptId { get; set; }

    [Column("ProductId")]
    public int ProductId { get; set; }

    [Column("Quantity")]
    public int Quantity { get; set; }

    [Column("ImportPrice")]
    public decimal ImportPrice { get; set; }

    public virtual GoodsReceipt GoodsReceipt { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
