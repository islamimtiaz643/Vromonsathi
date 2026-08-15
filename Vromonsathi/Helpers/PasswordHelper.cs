using System.Security.Cryptography;

namespace Vromonsathi.Helpers
{
    public static class PasswordHelper
    {
        // Creates a random salt and a hash of (password + salt) using PBKDF2
        public static void CreatePasswordHash(string password, out string hash, out string salt)
        {
            byte[] saltBytes = RandomNumberGenerator.GetBytes(16);
            salt = Convert.ToBase64String(saltBytes);

            byte[] hashBytes = Rfc2898DeriveBytes.Pbkdf2(
                password: System.Text.Encoding.UTF8.GetBytes(password),
                salt: saltBytes,
                iterations: 100000,
                hashAlgorithm: HashAlgorithmName.SHA256,
                outputLength: 32);

            hash = Convert.ToBase64String(hashBytes);
        }

        // Re-hashes the entered password with the stored salt and compares
        public static bool VerifyPassword(string enteredPassword, string storedHash, string storedSalt)
        {
            byte[] saltBytes = Convert.FromBase64String(storedSalt);

            byte[] hashBytes = Rfc2898DeriveBytes.Pbkdf2(
                password: System.Text.Encoding.UTF8.GetBytes(enteredPassword),
                salt: saltBytes,
                iterations: 100000,
                hashAlgorithm: HashAlgorithmName.SHA256,
                outputLength: 32);

            string computedHash = Convert.ToBase64String(hashBytes);
            return computedHash == storedHash;
        }
    }
}