using System;
using System.Security.Cryptography;
using System.Text;
using ContosoInsurance.Data.Models;

namespace ContosoInsurance.Data.Security;

/// <summary>
/// LEGACY: passwords hashed with SHA1 + per-user salt. Deliberately weak so
/// Copilot / appmod can recommend a modern KDF (PBKDF2 / Argon2 / Data Protection).
/// </summary>
public static class LegacyPasswordVerifier
{
    public static bool Verify(User user, string password)
    {
        if (user == null) return false;
        var candidate = HashSha1(password + user.Salt);
        return string.Equals(candidate, user.PasswordHash, StringComparison.OrdinalIgnoreCase);
    }

    private static string HashSha1(string input)
    {
        using (var sha = SHA1.Create()) // LEGACY: weak
        {
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }
}
