// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Security.Cryptography;
using System.Text;

namespace DWLibary
{
    /// <summary>
    /// Holds a secret encrypted while it is cached in memory, so tokens are never kept as
    /// clear text in the managed heap (crash dumps, memory scraping, accidental serialization).
    /// The key only lives for the lifetime of the process and is never written to disk.
    /// </summary>
    internal sealed class InMemorySecret
    {
        private static readonly byte[] key = RandomNumberGenerator.GetBytes(32);

        private byte[]? cipher;
        private byte[]? nonce;
        private byte[]? tag;

        public string? Value
        {
            get { return read(); }
            set { write(value); }
        }

        public void Clear()
        {
            if (cipher != null)
                CryptographicOperations.ZeroMemory(cipher);

            cipher = null;
            nonce = null;
            tag = null;
        }

        private void write(string? value)
        {
            Clear();

            if (value == null)
                return;

            byte[] plain = Encoding.UTF8.GetBytes(value);

            try
            {
                nonce = RandomNumberGenerator.GetBytes(AesGcm.NonceByteSizes.MaxSize);
                tag = new byte[AesGcm.TagByteSizes.MaxSize];
                cipher = new byte[plain.Length];

                using (AesGcm aes = new AesGcm(key, tag.Length))
                {
                    aes.Encrypt(nonce, plain, cipher, tag);
                }
            }
            finally
            {
                CryptographicOperations.ZeroMemory(plain);
            }
        }

        private string? read()
        {
            if (cipher == null || nonce == null || tag == null)
                return null;

            byte[] plain = new byte[cipher.Length];

            try
            {
                using (AesGcm aes = new AesGcm(key, tag.Length))
                {
                    aes.Decrypt(nonce, cipher, tag, plain);
                }

                return Encoding.UTF8.GetString(plain);
            }
            catch (CryptographicException)
            {
                return null;
            }
            finally
            {
                CryptographicOperations.ZeroMemory(plain);
            }
        }
    }
}
