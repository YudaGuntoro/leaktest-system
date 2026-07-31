using System.Security.Cryptography;
using System.Text;

namespace Web.API.Persistence.Services.Shared;

public static class AuthPasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 100000;

    public static string CreateSalt()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(SaltSize));
    }

    public static string HashPassword(string password, string salt)
    {
        var saltBytes = Convert.FromBase64String(salt);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            saltBytes,
            Iterations,
            HashAlgorithmName.SHA256,
            HashSize);

        return Convert.ToBase64String(hash);
    }

    public static bool VerifyPassword(string password, string salt, string expectedHash)
    {
        var hash = HashPassword(password, salt);
        var hashBytes = Convert.FromBase64String(hash);
        var expectedBytes = Convert.FromBase64String(expectedHash);

        return hashBytes.Length == expectedBytes.Length &&
               CryptographicOperations.FixedTimeEquals(hashBytes, expectedBytes);
    }
}
