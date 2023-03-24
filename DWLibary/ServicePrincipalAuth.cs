// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DWLibary
{

    //for future releases
    public  class ServicePrincipalAuth
    {
        ILogger logger;

        public ServicePrincipalAuth(ILogger _logger)
        {
            logger = _logger;
        }
        public async Task<bool> authenticate()
        {
            bool ret = false;
            try
            {
                var clientCredential = new ClientCredential(GlobalVar.username, GlobalVar.password);
                AuthenticationContext context = new AuthenticationContext($"https://login.microsoftonline.com/{GlobalVar.tenant}/", false);
                
                AuthenticationResult authenticationResult = await context.AcquireTokenAsync("https://IntegratorApp.com", clientCredential);
                ret = true;

                GlobalVar.loginData = new Struct.LoginData();
                GlobalVar.loginData.authResult = authenticationResult;

                GlobalVar.loginData.expires_in = Convert.ToInt32((authenticationResult.ExpiresOn - DateTime.UtcNow).TotalSeconds);

            }
            catch(Exception ex)
            {
                logger.LogError(ex.ToString());
            }

            return ret;

        }


    }
}
