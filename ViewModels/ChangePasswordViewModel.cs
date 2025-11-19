using System.ComponentModel.DataAnnotations;

namespace TourViet.ViewModels;

public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Mật khẩu hiện tại là bắt buộc")]
    [Display(Name = "Mật khẩu hiện tại")]
    [DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu mới là bắt buộc")]
    [Display(Name = "Mật khẩu mới")]
    [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Xác nhận mật khẩu mới là bắt buộc")]
    [Display(Name = "Xác nhận mật khẩu mới")]
    [DataType(DataType.Password)]
    [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}

