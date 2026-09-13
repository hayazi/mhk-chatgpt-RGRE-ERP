using System.Security.Cryptography;
using RGRE.ERP.Application.Abstractions;

namespace RGRE.ERP.Infrastructure.Security;

/// <summary>
/// PBKDF2 (SHA-256) password hashing. Storage format:
/// <c>iterations.saltBase64.hashBase64</c> so the iteration count can grow
/// without invalidating existing hashes.
/// </summary>
public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    public const int DefaultIterations = 100_000;

    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const char Separator = '.';

    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            DefaultIterations,
            HashAlgorithmName.SHA256,
            HashSize);

        return string.Create(
            null,
            stackalloc char[0],
            $"{DefaultIterations}{Separator}{Convert.ToBase64String(salt)}{Separator}{Convert.ToBase64String(hash)}");
    }

    public bool Verify(string password, string storedHash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedHash))
            return false;

        var parts = storedHash.Split(Separator);

        if (parts.Length != 3 || !int.TryParse(parts[0], out var iterations))
            return false;

        if (iterations < 1)
            return false;

        byte[] salt;
        byte[] expected;

        try
        {
            salt = Convert.FromBase64String(parts[1]);
            expected = Convert.FromBase64String(parts[2]);
        }
        catch (FormatException)
        {
            return false;
        }

        var actual = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            expected.Length);

        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
