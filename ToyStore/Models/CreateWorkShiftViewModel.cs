using System.ComponentModel.DataAnnotations;
using ToyStore.Helpers;

namespace ToyStore.Models;

public class CreateWorkShiftViewModel
{
    [Required(ErrorMessage = "Vui lòng chọn nhân viên")]
    [Display(Name = "Nhân viên")]
    public int AdminId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn ngày làm việc")]
    [DataType(DataType.Date)]
    [Display(Name = "Ngày làm việc")]
    public DateTime WorkDate { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Vui lòng chọn ca làm việc")]
    [Display(Name = "Ca làm việc")]
    public string ShiftName { get; set; } = HrConstants.ShiftMorning;
}
