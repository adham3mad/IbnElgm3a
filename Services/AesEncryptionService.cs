using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace IbnElgm3a.Services
{
    public interface IAesEncryptionService
    {
        string Encrypt(string plainText);
        string Decrypt(string cipherText);
        decimal EncryptDecimal(decimal value);
        decimal DecryptDecimal(decimal cipherValue);
        string EncryptDecimalToString(decimal value);
        decimal DecryptStringToDecimal(string cipherText);
    }

    public class AesEncryptionService : IAesEncryptionService
    {
        private readonly byte[] _key;

        public AesEncryptionService(string encryptionKey)
        {
            if (string.IsNullOrEmpty(encryptionKey))
            {
                // Fallback fixed key for development if env is missing (NEVER USE IN PRODUCTION WITHOUT ENV VAR)
                encryptionKey = "MasaarDefaultDbEncryptionKey1234567890="; 
            }
            
            // Always hash the input string to exactly 256 bits (32 bytes) 
            // to ensure it is always a valid size for AES-256 regardless of input format.
            _key = SHA256.HashData(Encoding.UTF8.GetBytes(encryptionKey));
        }

        public string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return plainText;

            using Aes aes = Aes.Create();
            aes.Key = _key;
            
            // Generate deterministic IV for equality search in DB
            using (var hmac = new HMACSHA256(_key))
            {
                byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(plainText));
                byte[] iv = new byte[16]; // AES BlockSize is 128 bit = 16 bytes
                Array.Copy(hash, iv, 16);
                aes.IV = iv;
            }

            using MemoryStream ms = new MemoryStream();
            // Prefix output with IV
            ms.Write(aes.IV, 0, aes.IV.Length);

            using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
            using (StreamWriter sw = new StreamWriter(cs))
            {
                sw.Write(plainText);
            }

            return Convert.ToBase64String(ms.ToArray());
        }

        public string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText)) return cipherText;

            try
            {
                byte[] fullCipher = Convert.FromBase64String(cipherText);

                using Aes aes = Aes.Create();
                byte[] iv = new byte[aes.BlockSize / 8];
                byte[] cipher = new byte[fullCipher.Length - iv.Length];

                Array.Copy(fullCipher, iv, iv.Length);
                Array.Copy(fullCipher, iv.Length, cipher, 0, cipher.Length);

                aes.Key = _key;
                aes.IV = iv;

                using MemoryStream ms = new MemoryStream(cipher);
                using CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
                using StreamReader sr = new StreamReader(cs);

                return sr.ReadToEnd();
            }
            catch
            {
                // If decryption fails (e.g., data was not encrypted), return original data
                return cipherText; 
            }
        }

        // For Decimals, we convert to string, encrypt, and base64 store it. 
        // EF Core ValueConverter allows string columns in DB but Decimal in entity.
        public string EncryptDecimalToString(decimal value)
        {
            return Encrypt(value.ToString("G"));
        }

        public decimal DecryptStringToDecimal(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText)) return 0m;
            var decrypted = Decrypt(cipherText);
            if (decimal.TryParse(decrypted, out decimal val))
                return val;
            return 0m; 
        }

        // Methods if storing as raw decimals isn't desirable via strings.
        // EF Core mapping to string in DB is infinitely easier and safer for AES outputs.
        public decimal EncryptDecimal(decimal value) => throw new NotImplementedException("Use EncryptDecimalToString with string backing field");
        public decimal DecryptDecimal(decimal cipherValue) => throw new NotImplementedException("Use DecryptStringToDecimal with string backing field");
    }
}
