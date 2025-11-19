using TourViet.DTOs.Requests;
using TourViet.DTOs.Responses;
using TourViet.Models;
using TourViet.ViewModels;

namespace TourViet.Extensions;

public static class MappingExtensions
{
    public static RegisterRequest ToRequest(this RegisterViewModel viewModel)
    {
        return new RegisterRequest
        {
            FullName = viewModel.FullName,
            Username = viewModel.Username,
            Email = viewModel.Email,
            Phone = viewModel.Phone,
            Password = viewModel.Password,
            ConfirmPassword = viewModel.ConfirmPassword
        };
    }

    public static LoginRequest ToRequest(this LoginViewModel viewModel)
    {
        return new LoginRequest
        {
            Email = viewModel.Email,
            Password = viewModel.Password,
            RememberMe = viewModel.RememberMe
        };
    }

    public static ChangePasswordRequest ToRequest(this ChangePasswordViewModel viewModel)
    {
        return new ChangePasswordRequest
        {
            CurrentPassword = viewModel.CurrentPassword,
            NewPassword = viewModel.NewPassword,
            ConfirmNewPassword = viewModel.ConfirmNewPassword
        };
    }
}

