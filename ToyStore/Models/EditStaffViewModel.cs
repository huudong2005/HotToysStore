using System.ComponentModel.DataAnnotations;

namespace ToyStore.Models;

/// <summary>
/// ViewModel sửa nhân viên — mật khẩu tùy chọn (để trống = giữ nguyên).
/// </summary>
public class EditStaffViewModel : IValidatableObject
{
    [Required(ErrorMessage = "Họ tên là bắt buộc")]
    [StringLength(100, ErrorMessage = "Họ tên không được vượt quá 100 ký tự")]
    [Display(Name = "Họ tên")]
    public string FullName { get; set; } = null!;

    [Required(ErrorMessage = "Username là bắt buộc")]
    [StringLength(50, ErrorMessage = "Username không được vượt quá 50 ký tự")]
    [Display(Name = "Username")]
    public string Username { get; set; } = null!;

    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu")]
    public string? Password { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Xác nhận mật khẩu")]
    public string? ConfirmPassword { get; set; }

    [Required(ErrorMessage = "Vai trò là bắt buộc")]
    [Display(Name = "Vai trò")]
    public string Role { get; set; } = "Staff";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var hasPassword = !string.IsNullOrWhiteSpace(Password);
        var hasConfirm = !string.IsNullOrWhiteSpace(ConfirmPassword);

        if (!hasPassword && !hasConfirm)
        {
            yield break;
        }

        if (hasPassword && !hasConfirm)
        {
            yield return new ValidationResult(
                "Vui lòng xác nhận mật khẩu mới.",
                new[] { nameof(ConfirmPassword) });
        }

        if (!hasPassword && hasConfirm)
        {
            yield return new ValidationResult(
                "Vui lòng nhập mật khẩu mới.",
                new[] { nameof(Password) });
        }

        if (hasPassword && hasConfirm && Password != ConfirmPassword)
        {
            yield return new ValidationResult(
                "Mật khẩu xác nhận không khớp.",
                new[] { nameof(ConfirmPassword) });
        }

        if (hasPassword && Password!.Length < 6)
        {
            yield return new ValidationResult(
                "Mật khẩu phải có ít nhất 6 ký tự.",
                new[] { nameof(Password) });
        }
    }
}
