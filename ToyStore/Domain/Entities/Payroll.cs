using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ToyStore.Domain.Entities;

[Table("Payroll")]
public partial class Payroll
{
    [Key]
    [Column("PayrollId")]
    public int PayrollId { get; set; }

    [Column("AdminId")]
    public int AdminId { get; set; }

    [Column("Month")]
    public int Month { get; set; }

    [Column("Year")]
    public int Year { get; set; }

    [Column("BasicSalaryPerShift")]
    public decimal BasicSalaryPerShift { get; set; }

    [Column("TotalShifts")]
    public int TotalShifts { get; set; }

    [Column("NetSalary")]
    public decimal NetSalary { get; set; }

    [Column("Status")]
    public string Status { get; set; } = null!;

    public virtual Admin Admin { get; set; } = null!;
}
