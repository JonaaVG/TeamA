using System.Security.Cryptography;
using Microsoft.AspNetCore.WebUtilities;

namespace GentleHealersApi.Extensions
{
    public static class TokenHelper
    {
        // Generates a secure random token
        public static string GenerateRawToken()
        {
            var randomBytes = RandomNumberGenerator.GetBytes(32);
            return WebEncoders.Base64UrlEncode(randomBytes);
        }

        // Hashes token with BCrypt
        public static string HashToken(string rawToken)
        {
            return BCrypt.Net.BCrypt.HashPassword(rawToken);
        }

        // Compares incoming token with the stored hash token
        public static bool VerifyToken(string rawToken, string tokenHash)
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(rawToken, tokenHash);
            }
            catch
            {
                return false;
            }
        }
    }
}