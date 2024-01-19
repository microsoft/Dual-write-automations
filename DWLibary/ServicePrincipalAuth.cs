// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
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

                var credential = new UsernamePasswordCredential(GlobalVar.username, GlobalVar.password, GlobalVar.parsedOptions.tenant, GlobalVar.parsedOptions.clientId);
                var token = await credential.GetTokenAsync(new TokenRequestContext(new[] { "https://IntegratorApp.com/.default" }));

                GlobalVar.loginData.accessToken = token;

            }
            catch(Exception ex)
            {
                logger.LogError(ex.ToString());
            }

            return ret;

        }



    }
}
