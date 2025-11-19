using System.Text.RegularExpressions;

namespace TourViet.Services;

public class PasswordStrengthValidator : IPasswordStrengthValidator
{
    public PasswordStrengthResult Validate(string password)
    {
        var result = new PasswordStrengthResult();
        var score = 0;

        if (string.IsNullOrWhiteSpace(password))
        {
            result.Errors.Add("Mật khẩu không được để trống");
            return result;
        }

        // Length checks
        if (password.Length < 6)
        {
            result.Errors.Add("Mật khẩu phải có ít nhất 6 ký tự");
        }
        else if (password.Length >= 8)
        {
            score += 2;
        }
        else
        {
            score += 1;
        }

        // Uppercase check
        if (Regex.IsMatch(password, @"[A-Z]"))
        {
            score += 1;
        }
        else
        {
            result.Errors.Add("Mật khẩu nên có ít nhất một chữ hoa");
        }

        // Lowercase check
        if (Regex.IsMatch(password, @"[a-z]"))
        {
            score += 1;
        }

        // Digit check
        if (Regex.IsMatch(password, @"[0-9]"))
        {
            score += 1;
        }
        else
        {
            result.Errors.Add("Mật khẩu nên có ít nhất một số");
        }

        // Special character check
        if (Regex.IsMatch(password, @"[!@#$%^&*(),.?\"":{}|<>]"))
        {
            score += 1;
        }
        else
        {
            result.Errors.Add("Mật khẩu nên có ít nhất một ký tự đặc biệt");
        }

        // Determine strength
        result.Score = score;
        result.Strength = score switch
        {
            >= 5 => "Mạnh",
            >= 3 => "Trung bình",
            _ => "Yếu"
        };

        // Minimum requirements: at least 6 chars and score >= 2
        result.IsValid = password.Length >= 6 && score >= 2;

        return result;
    }
}

