namespace TourViet.Services;

public class PasswordStrengthResult
{
    public bool IsValid { get; set; }
    public int Score { get; set; }
    public List<string> Errors { get; set; } = new();
    public string Strength { get; set; } = string.Empty;
}

public interface IPasswordStrengthValidator
{
    PasswordStrengthResult Validate(string password);
}

