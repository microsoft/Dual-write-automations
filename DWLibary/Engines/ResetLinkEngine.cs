using DWLibary.Struct;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DWLibary.Engines
{
    public class ResetLinkEngine
    {
        DWCommonEngine common;
        DWEnvironment env;
        ILogger logger;
        ResetLinkPayload payload;
        bool forceReset;

        public ResetLinkEngine(ILogger _logger, DWEnvironment _env)
        {
            this.logger = _logger;
            this.env = _env;
            payload = new ResetLinkPayload();
            common = new DWCommonEngine(env, logger);
        }


        public async Task resetLink(bool force = false)

        {

            forceReset = force;
            await common.getConnectionSet();

            await buildEnvironments();

            getAddCurrentLegalEntities();

            await sendResetLinkPayload();

        }

        public async Task sendResetLinkPayload()
        {
            //connectionSet is only needed once

            logger.LogInformation($"Reset Link");
            try
            {
                HttpClient client = new HttpClientWithRetry();
                client.Timeout = new TimeSpan(0,0,300);
                DWHttp dW = new DWHttp(env);

                HttpRequestMessage req = dW.buildDefaultHttpRequestPost();

                UriBuilder uriBuilder = new UriBuilder(req.RequestUri.Scheme + "://" + req.RequestUri.Host);
                uriBuilder.Path += $"api/ConnectionSet/{env.cid}/Reset";
                uriBuilder.Query = $"targetType=AX&forceReset={forceReset}";

                req.RequestUri = uriBuilder.Uri;


                string payloadStr = JsonConvert.SerializeObject(payload, Newtonsoft.Json.Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                logger.LogDebug($"ResetLinkPayload: {payloadStr}");

                req.Content = new StringContent(payloadStr, Encoding.UTF8, "application/json");

                //Debug Logging >>
                logger.LogDebug($"Request URI: {req.RequestUri}");
                //Debug Logging <<
                logger.LogInformation("Sending ResetLink request, this can take longer...");
                var responseStr = await client.SendAsync(req);

                string content = await responseStr.Content.ReadAsStringAsync();

                //Debug Logging >>
                logger.LogDebug($"Response: {responseStr} {content}");
                //Debug Logging <<

                if(responseStr.IsSuccessStatusCode)
                {
                    logger.LogInformation("Successful reset");
                }
                else
                {
                    logger.LogError("Reset link failed");
                }



            }
            catch (Exception ex)
            {
                logger.LogError(ex.ToString());
            }
        }


        private void getAddCurrentLegalEntities()
        {
            List<string> ret = new List<string>();

            foreach(var le in common.connectionSet.dualWriteDetail.legalEntityMappings.mappings)
            {
                if(!ret.Contains(le.left.name))
                {
                    ret.Add(le.left.name);
                }
            }

            payload.legalEntities = ret;

            //return ret;

        }

        private async Task buildEnvironments()
        {
            payload.environments = new List<ResetLinkEnvironment>();

            
            DWConnSetEnvironment ceEnv = await common.getConnectionSetEnvironment(DWEnums.DataMaster.CE);
            DWConnSetEnvironment foEnv = await common.getConnectionSetEnvironment(DWEnums.DataMaster.FO);

            payload.environments.Add(mapEnvToResetLinkEnv(ceEnv));
            payload.environments.Add(mapEnvToResetLinkEnv(foEnv));

            //adding power apps env:

            payload.powerAppsEnvironmentName = ceEnv.powerAppsEnvironment;

        }

        private ResetLinkEnvironment mapEnvToResetLinkEnv(DWConnSetEnvironment _env)
        {
            ResetLinkEnvironment ret = new ResetLinkEnvironment();

            ret.name = _env.name;
            ret.displayName = _env.environmentDisplayName;
            ret.id = _env.powerAppsEnvironment;
            ret.isDevInstance = _env.isDevInstance;
            ret.targetType = _env.targetType;
            ret.directUrl = _env.directUrl;
            return ret;

        }


    }
}
