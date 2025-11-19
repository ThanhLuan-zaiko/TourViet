namespace TourViet.Services;

public interface IPasswordHasher
{
    (byte[] hash, byte[] salt) HashPassword(string password);
    bool VerifyPassword(string password, byte[] hash, byte[] salt);
}

