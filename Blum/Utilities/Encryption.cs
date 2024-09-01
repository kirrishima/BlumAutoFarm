using System.Security.Cryptography;
using System.Text;
using Blum.Exceptions;

namespace Blum.Utilities
{
    public class Encryption
    {
        private readonly byte[] _key;
        private readonly byte[] _iv;

        public Encryption(string key)
        {
            BlumException.ThrowIfNull(key);
            _key = SHA256.HashData(Encoding.UTF8.GetBytes(key));
            _iv = new byte[16];
        }

        public string Encrypt(string plainText)
        {
            BlumException.ThrowIfNull(plainText);

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = _key;
                aesAlg.IV = _iv;

                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msEncrypt = new())
                {
                    using (CryptoStream csEncrypt = new(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new(csEncrypt))
                        {
                            swEncrypt.Write(plainText);
                        }
                    }
                    return Convert.ToBase64String(msEncrypt.ToArray());
                }
            }
        }

        public string Decrypt(string cipherTextBase64)
        {
            BlumException.ThrowIfNull(cipherTextBase64);

            byte[] cipherText = Convert.FromBase64String(cipherTextBase64);

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = _key;
                aesAlg.IV = _iv;

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            return srDecrypt.ReadToEnd();
                        }
                    }
                }
            }
        }
    }

}