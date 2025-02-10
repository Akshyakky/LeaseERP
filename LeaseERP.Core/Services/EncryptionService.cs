using LeaseERP.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;

namespace LeaseERP.Core.Services
{
    public class EncryptionService : IEncryptionService
    {
        private readonly byte[] _key;
        private readonly byte[] _iv;

        public EncryptionService(IConfiguration configuration)
        {
            var encryptionKey = configuration["AppSettings:EncryptionKey"]
                ?? throw new ArgumentNullException("Encryption key not configured");
            var initVector = configuration["AppSettings:InitializationVector"]
                ?? throw new ArgumentNullException("Initialization vector not configured");

            _key = Convert.FromBase64String(encryptionKey);
            _iv = Convert.FromBase64String(initVector);
        }

        public string HashPassword(string password)
        {
            // Generate a random salt
            byte[] salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            // Hash the password with the salt using PBKDF2
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256);
            byte[] hash = pbkdf2.GetBytes(32);

            // Combine salt and hash
            byte[] hashBytes = new byte[48];
            Array.Copy(salt, 0, hashBytes, 0, 16);
            Array.Copy(hash, 0, hashBytes, 16, 32);

            return Convert.ToBase64String(hashBytes);
        }

        public bool VerifyPassword(string password, string hashedPassword)
        {
            byte[] hashBytes = Convert.FromBase64String(hashedPassword);

            // Extract salt and hash
            byte[] salt = new byte[16];
            byte[] hash = new byte[32];
            Array.Copy(hashBytes, 0, salt, 0, 16);
            Array.Copy(hashBytes, 16, hash, 0, 32);

            // Generate hash from the provided password
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256);
            byte[] newHash = pbkdf2.GetBytes(32);

            // Compare the hashes
            return hash.SequenceEqual(newHash);
        }

        public string EncryptSensitiveData(string plainText)
        {
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;

            using var encryptor = aes.CreateEncryptor();
            using var msEncrypt = new MemoryStream();
            using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
            using (var swEncrypt = new StreamWriter(csEncrypt))
            {
                swEncrypt.Write(plainText);
            }

            return Convert.ToBase64String(msEncrypt.ToArray());
        }

        public string DecryptSensitiveData(string cipherText)
        {
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;

            using var decryptor = aes.CreateDecryptor();
            using var msDecrypt = new MemoryStream(Convert.FromBase64String(cipherText));
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var srDecrypt = new StreamReader(csDecrypt);

            return srDecrypt.ReadToEnd();
        }
    }
}
