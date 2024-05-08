// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using DWLibary.Struct;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DWLibary
{
    public  class DWEnvCalls
    {
        DWEnvironment env;


        public async Task<DWEnvironment> getEnvironment()
        {
            DWEnvironment environment = new DWEnvironment();
            try
            {
                HttpClient client = new HttpClientWithRetry();
                DWHttp dW = new DWHttp();
                HttpRequestMessage req = dW.buildDefaultHttpRequestGet();

                UriBuilder uriBuilder = new UriBuilder(req.RequestUri);
                uriBuilder.Path += $"Environments";
                uriBuilder.Query = $"targetType=AX&identifier={GlobalVar.foEnv}";

                req.RequestUri = uriBuilder.Uri;

                var response = await client.SendAsync(req);

                string content = await response.Content.ReadAsStringAsync();
                environment = JsonConvert.DeserializeObject<List<DWEnvironment>>(content)[0];
                environment.foEnvironment = GlobalVar.foEnv;



            }
            catch (Exception ex)
            {
                
            }

            return environment;

        }


    }
}
