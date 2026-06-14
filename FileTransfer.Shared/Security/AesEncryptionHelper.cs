using System;
using System.Configuration;
using System.IO;
using System.Security.Cryptography;

namespace FileTransfer.Shared.Security
{
    public static class AesEncryptionHelper
    {
        private static readonly byte[] Key;
        private static readonly byte[] IV;

        static AesEncryptionHelper()
        {
            string keyBase64 = Environment.GetEnvironmentVariable("FT_AES_KEY")
                ?? ConfigurationManager.AppSettings["AesKey"];

            string ivBase64 = Environment.GetEnvironmentVariable("FT_AES_IV")
                ?? ConfigurationManager.AppSettings["AesIV"];

            if (string.IsNullOrWhiteSpace(keyBase64))
            {
                throw new InvalidOperationException(
                    "FT_AES_KEY environment variable or " +
                    "AesKey App.config value is not set. " +
                    "Set a 32-byte (44 base64 chars) AES-256 key " +
                    "before using encryption.");
            }

            if (string.IsNullOrWhiteSpace(ivBase64))
            {
                throw new InvalidOperationException(
                    "FT_AES_IV environment variable or " +
                    "AesIV App.config value is not set. " +
                    "Set a 16-byte (24 base64 chars) AES IV " +
                    "before using encryption.");
            }

            try
            {
                Key = Convert.FromBase64String(keyBase64);
            }
            catch (FormatException)
            {
                throw new InvalidOperationException(
                    "AES key is not valid Base64. " +
                    "Generate a 32-byte key and encode as Base64.");
            }

            try
            {
                IV = Convert.FromBase64String(ivBase64);
            }
            catch (FormatException)
            {
                throw new InvalidOperationException(
                    "AES IV is not valid Base64. " +
                    "Generate a 16-byte IV and encode as Base64.");
            }

            if (Key.Length != 32)
            {
                throw new InvalidOperationException(
                    "AES key must be exactly 32 bytes " +
                    "(44 Base64 characters). Current length: "
                    + Key.Length + " bytes.");
            }

            if (IV.Length != 16)
            {
                throw new InvalidOperationException(
                    "AES IV must be exactly 16 bytes " +
                    "(24 Base64 characters). Current length: "
                    + IV.Length + " bytes.");
            }
        }

        public static byte[] Encrypt(byte[] data)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = Key;
                aes.IV = IV;

                using (MemoryStream ms =
                    new MemoryStream())
                {
                    using (CryptoStream cs =
                        new CryptoStream(
                            ms,
                            aes.CreateEncryptor(),
                            CryptoStreamMode.Write))
                    {
                        cs.Write(data, 0, data.Length);
                        cs.FlushFinalBlock();

                        return ms.ToArray();
                    }
                }
            }
        }

        public static byte[] Decrypt(byte[] encryptedData)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = Key;
                aes.IV = IV;

                using (MemoryStream ms =
                    new MemoryStream())
                {
                    using (CryptoStream cs =
                        new CryptoStream(
                            ms,
                            aes.CreateDecryptor(),
                            CryptoStreamMode.Write))
                    {
                        cs.Write(
                            encryptedData,
                            0,
                            encryptedData.Length);

                        cs.FlushFinalBlock();

                        return ms.ToArray();
                    }
                }
            }
        }
    }
}