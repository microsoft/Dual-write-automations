using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DWLibary
{
    public class EncryptionKeyGenerator
    {
        

        public static List<string>  GenerateAndStoreKeys()
        {
            List<string> keyiv = new List<string>();

            using (Aes aes = Aes.Create())
            {
                aes.KeySize = 256; // 256 bits for AES-256
                aes.GenerateKey();
                aes.GenerateIV();

                byte[] protectedKey = ProtectedData.Protect(aes.Key, null, DataProtectionScope.CurrentUser);
                byte[] protectedIV = ProtectedData.Protect(aes.IV, null, DataProtectionScope.CurrentUser);

                keyiv.Add(Convert.ToBase64String(protectedKey));
                keyiv.Add(Convert.ToBase64String(protectedIV));
                

            }

            return keyiv;
        }

    }
}
