// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OtpNet;

namespace DWLibary
{
    public class MFAGen
    {   
        public static string getMFAKey()
        {
            string secret = string.Empty;

            if (GlobalVar.mfasecret != String.Empty)
                secret = GlobalVar.mfasecret;
            else
                secret = GlobalVar.config.AppSettings.Settings["MFASecretKey"].Value;

            var otpKeyBytes = Base32Encoding.ToBytes(secret);
            var totp = new Totp(otpKeyBytes);
            var twoFactorCode = totp.ComputeTotp(); // <- got 2FA coed at this time!

            return twoFactorCode;
        }
    }
}
