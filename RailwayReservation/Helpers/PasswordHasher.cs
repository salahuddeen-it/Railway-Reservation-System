using System;
using System.Security.Cryptography;

namespace RailwayReservation.Helpers
{
    /// <summary>
    /// Secure password hashing using PBKDF2 (HMAC-SHA256, 100,000 iterations, random salt).
    /// Passwords are never stored or compared in plain text.
    /// </summary>
    public static class PasswordHasher
    {
        private const int SaltSize = 16;
        private const int KeySize = 32;
        private const int Iterations = 100000;

        // Creates hash + salt (Base64) for a new password
        public static void CreateHash(string password, out string hash, out string salt)
        {
            byte[] saltBytes = new byte[SaltSize];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }

            using (var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, Iterations, HashAlgorithmName.SHA256))
            {
                hash = Convert.ToBase64String(pbkdf2.GetBytes(KeySize));
            }
            salt = Convert.ToBase64String(saltBytes);
        }

        // Verifies an entered password against the stored hash + salt
        public static bool Verify(string password, string storedHash, string storedSalt)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedHash) || string.IsNullOrEmpty(storedSalt))
                return false;

            byte[] saltBytes;
            byte[] expected;
            try
            {
                saltBytes = Convert.FromBase64String(storedSalt);
                expected = Convert.FromBase64String(storedHash);
            }
            catch (FormatException)
            {
                return false;
            }

            using (var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, Iterations, HashAlgorithmName.SHA256))
            {
                byte[] actual = pbkdf2.GetBytes(expected.Length);
                return FixedTimeEquals(expected, actual);
            }
        }

        // Generates a random alphanumeric default password (at least 1 letter + 1 digit)
        public static string GenerateDefaultPassword(int length = 10)
        {
            const string letters = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz";
            const string digits = "23456789";
            const string all = letters + digits;

            char[] chars = new char[length];
            using (var rng = RandomNumberGenerator.Create())
            {
                chars[0] = letters[RandomIndex(rng, letters.Length)];
                chars[1] = digits[RandomIndex(rng, digits.Length)];
                for (int i = 2; i < length; i++)
                    chars[i] = all[RandomIndex(rng, all.Length)];

                // Fisher-Yates shuffle so letter/digit positions are random
                for (int i = length - 1; i > 0; i--)
                {
                    int j = RandomIndex(rng, i + 1);
                    char tmp = chars[i];
                    chars[i] = chars[j];
                    chars[j] = tmp;
                }
            }
            return new string(chars);
        }

        // Cryptographically secure random index in range [0, max)
        private static int RandomIndex(RandomNumberGenerator rng, int max)
        {
            byte[] buffer = new byte[4];
            rng.GetBytes(buffer);
            return (int)(BitConverter.ToUInt32(buffer, 0) % (uint)max);
        }

        // Constant-time comparison (prevents timing attacks)
        private static bool FixedTimeEquals(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++)
                diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }
}