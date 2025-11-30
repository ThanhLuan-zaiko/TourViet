using System.Security.Cryptography;
using System.Text;

namespace TourViet.Services;

public class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 32;
    private const int Iterations = 1000;
    private const int HashSize = 64; // SHA2_512 produces 64 bytes

    public (byte[] hash, byte[] salt) HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be null or empty", nameof(password));

        // Generate salt
        var salt = new byte[SaltSize];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }

        // Hash password with salt
        var hash = ComputeHash(password, salt);

        return (hash, salt);
    }

    public bool VerifyPassword(string password, byte[] hash, byte[] salt)
    {
        if (string.IsNullOrWhiteSpace(password))
            return false;

        if (hash == null || hash.Length != HashSize)
            return false;

        if (salt == null || salt.Length != SaltSize)
            return false;

        var computedHash = ComputeHash(password, salt);
        return CryptographicOperations.FixedTimeEquals(hash, computedHash);
    }

    private byte[] ComputeHash(string password, byte[] salt)
    {
        using var sha512 = SHA512.Create();
        var passwordBytes = Encoding.Unicode.GetBytes(password);
        var combined = new byte[passwordBytes.Length + salt.Length];
        
        Buffer.BlockCopy(passwordBytes, 0, combined, 0, passwordBytes.Length);
        Buffer.BlockCopy(salt, 0, combined, passwordBytes.Length, salt.Length);

        var hash = sha512.ComputeHash(combined);

        // Apply iterations (similar to SQL implementation)
        for (int i = 0; i < Iterations - 1; i++)
        {
            hash = sha512.ComputeHash(hash);
        }

        return hash;
    }
}

