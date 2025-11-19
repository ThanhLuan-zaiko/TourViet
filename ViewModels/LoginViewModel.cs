using System.ComponentModel.DataAnnotations;

namespace TourViet.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Email là bắt buộc")]
    [Display(Name = "Email")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
    [Display(Name = "Mật khẩu")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Ghi nhớ đăng nhập")]
    public bool RememberMe { get; set; }
}

