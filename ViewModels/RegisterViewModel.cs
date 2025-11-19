using System.ComponentModel.DataAnnotations;

namespace TourViet.ViewModels;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Họ và tên là bắt buộc")]
    [Display(Name = "Họ và tên")]
    [MaxLength(100, ErrorMessage = "Họ và tên không được vượt quá 100 ký tự")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tên đăng nhập là bắt buộc")]
    [Display(Name = "Tên đăng nhập")]
    [MaxLength(50, ErrorMessage = "Tên đăng nhập không được vượt quá 50 ký tự")]
    [MinLength(3, ErrorMessage = "Tên đăng nhập phải có ít nhất 3 ký tự")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email là bắt buộc")]
    [Display(Name = "Email")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    [MaxLength(100, ErrorMessage = "Email không được vượt quá 100 ký tự")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
    [Display(Name = "Số điện thoại")]
    [MaxLength(20, ErrorMessage = "Số điện thoại không được vượt quá 20 ký tự")]
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
    [Display(Name = "Mật khẩu")]
    [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Xác nhận mật khẩu là bắt buộc")]
    [Display(Name = "Xác nhận mật khẩu")]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

