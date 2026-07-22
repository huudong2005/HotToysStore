using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ToyStore.Domain.Entities;

[Table("WorkShift")]
public partial class WorkShift
{
    [Key]
    [Column("ShiftId")]
    public int ShiftId { get; set; }

    [Column("AdminId")]
    public int AdminId { get; set; }

    [Column("WorkDate")]
    public DateTime WorkDate { get; set; }

    [Column("ShiftName")]
    public string ShiftName { get; set; } = null!;

    [Column("Status")]
    public string Status { get; set; } = null!;

    [Column("Note")]
    public string? Note { get; set; }

    public virtual Admin Admin { get; set; } = null!;
}
