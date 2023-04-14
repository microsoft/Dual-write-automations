// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using DWLibary.Struct;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static Azure.Core.HttpHeader;

namespace DWLibary.Engines
{
    public class DWCommonEngine
    {
        DWEnvironment env;
        ILogger logger;
        public DWConnectionSet connectionSet;
        public DWFieldMapping curFieldMapping;

        public DWCommonEngine(DWEnvironment dwEnv, ILogger _logger)
        {
            env = dwEnv;
            logger = _logger;
            curFieldMapping = new DWFieldMapping();
            connectionSet = new DWConnectionSet();
        }


        public async Task<List<DWMap>> getDWMaps()
        {

            List<DWMap> ret = await tryGetDWMaps();
           

            return ret;
        }


        private async Task<List<DWMap>> tryGetDWMaps()
        {
            List<DWMap> ret = new List<DWMap>();
            int retryCount = 5;
            int currentRetry = 0;

            while (ret == null || !ret.Any())
            {
                currentRetry++;

                if (currentRetry > 1)
                {
                    logger.LogInformation("Could not retrieve maps, retrying in some time...");

                    Thread.Sleep(10000 * currentRetry);
                }

                try
                {

                    logger.LogDebug("Try to get list of maps...");
                    HttpClient client = new HttpClient();
                    DWHttp dW = new DWHttp();

                    HttpRequestMessage req = dW.buildDefaultHttpRequestGet();

                    UriBuilder uriBuilder = new UriBuilder(req.RequestUri);
                    uriBuilder.Path += $"Entities";
                    uriBuilder.Query = $"targetType=AX&cid={env.cid}";

                    req.RequestUri = uriBuilder.Uri;

                    //Debug Logging >>
                    logger.LogDebug($"Request URI: {req.RequestUri}");
                    //Debug Logging <<

                    var responseStr = await client.SendAsync(req);

                    string content = await responseStr.Content.ReadAsStringAsync();

                    //Debug Logging >>
                    logger.LogDebug($"Response: {responseStr} {content}");
                    //Debug Logging <<

                    ret = JsonConvert.DeserializeObject<List<DWMap>>(content);

                    //if(!ret.Any())
                    //{
                    //    string test = "";
                    //}
                }
                catch (Exception ex)
                {
                    GlobalVar.addError(ex.ToString());
                }

                if(retryCount == currentRetry)
                {
                     
                    if(ret == null || !ret.Any())
                    {

                        //re authenticate
                        logger.LogInformation("Get refresh token");
                        TokenRefresh refresh = new TokenRefresh(logger);
                        await refresh.getRefreshToken();
                        

                        GlobalVar.addError("Could not get map details after 5 retries");
                        //logger.LogError("Could not get map details");
                    }

                    
                }
                

            }

            return ret;
        }


        public async Task<DWConnSetEnvironment> getConnectionSetEnvironment(DWEnums.DataMaster data)
        {
            DWConnSetEnvironment ret = new DWConnSetEnvironment();

            if (connectionSet.id == null)
                await getConnectionSet();

            foreach (var env in connectionSet.environments)
            {
                if (env.Value.targetType == "CRM" && data == DWEnums.DataMaster.CE)
                {
                    ret = env.Value;
                    break;
                }

                if (env.Value.targetType == "AX" && data == DWEnums.DataMaster.FO)
                {
                    ret = env.Value;
                    break;
                }
            }

            return ret;

        }


        public async Task<string> getCurrentKeys(DWMap currentMap)
        {
            string ret = String.Empty;

            Key key = await getIntegrationKey(currentMap);

            if (key.displayName != null)
                ret = string.Join(",", key.fields);

            return ret;
        }

        private async Task<Key> getIntegrationKey(DWMap currentMap)
        {
            Key ret = new Key();

            if (connectionSet.id == null || connectionSet.id == String.Empty)
                await getConnectionSet();

            DWConnSetEnvironment curEnv = await getConnectionSetEnvironment(DWEnums.DataMaster.CE);

            //if (currentMap.detail.tName.Contains("curren"))
            //    logger.LogInformation("Test");

            if (curEnv.connectionSetName != null)
            {
                Schema schema = curEnv.schemas.Where(x => x.name.Equals(currentMap.rightEntity.name)).FirstOrDefault();

                if (schema.id != null)
                {
                    ret = schema.keys.Where(x => x.name.ToUpper().Equals("USERKEYS")).FirstOrDefault();

                    //If its just one single key we need this:
                    if(ret.displayName == null)
                        ret = schema.keys.Where(x => x.name.ToUpper().Equals("USERKEY")).FirstOrDefault();
                }
            }

            return ret;
        }


        public async Task<List<string>> getCurrentKeyList(DWMap currentMap)
        {
            List<string> ret = new List<string>();

            Key key = await getIntegrationKey(currentMap);

            if (key.displayName != null)
                ret = key.fields;

            return ret;
        }

        public async Task<List<Solution>> getSolutions()
        {
            List<Solution> ret = new List<Solution>();

            logger.LogInformation($"Retrieving Solutions");
            HttpClient client = new HttpClient();
            DWHttp dW = new DWHttp();

         

            HttpRequestMessage req = dW.buildDefaultHttpRequestGet();
            UriBuilder uriBuilder = new UriBuilder(req.RequestUri);
            uriBuilder.Path += $"SolutionAware/{env.cid}/Solutions/";

            req.RequestUri = uriBuilder.Uri;

            //Debug Logging >>
            logger.LogDebug($"Request URI: {req.RequestUri}");
            //Debug Logging <<

            var responseStr = await client.SendAsync(req);

            string content = await responseStr.Content.ReadAsStringAsync();

            //Debug Logging >>
            logger.LogDebug($"Response: {responseStr} {content}");
            //Debug Logging <<
            if (responseStr.IsSuccessStatusCode)
            {
                var solutions = JsonConvert.DeserializeObject<List<DWSolution>>(content);

                if (solutions != null)
                {
                    foreach (var solution in solutions)
                    {
                        Solution retSol = new Solution();
                        retSol.Name = solution.uniquename;

                        ret.Add(retSol);
                    }
                }
            }
            return ret;
        }


        public async Task getFieldMappingForMaps(DWMap currentMap, string prefix = "")
        {
            curFieldMapping = new DWFieldMapping();
            try
            {
                logger.LogInformation($"{prefix} - Retrieving field mappings");
                HttpClient client = new HttpClient();
                DWHttp dW = new DWHttp();

                HttpRequestMessage req = dW.buildDefaultHttpRequestGet();

                UriBuilder uriBuilder = new UriBuilder(req.RequestUri);
                uriBuilder.Path += $"{currentMap.detail.pid}/FieldMappings";
                req.RequestUri = uriBuilder.Uri;

                //Debug Logging >>
                logger.LogDebug($"Request URI: {req.RequestUri}");
                //Debug Logging <<

                var responseStr = await client.SendAsync(req);

                string content = await responseStr.Content.ReadAsStringAsync();

                //Debug Logging >>
                logger.LogDebug($"Response: {responseStr} {content}");
                //Debug Logging <<


                curFieldMapping = JsonConvert.DeserializeObject<DWFieldMapping>(content);



            }
            catch (Exception ex)
            {
                GlobalVar.addError(ex.ToString());
            }
        }

        public async Task getConnectionSet(bool force = false)
        {
            //connectionSet is only needed once
            if (connectionSet.id != null && connectionSet.id != String.Empty && !force)
                return;

            logger.LogInformation($"Get Connection set");
            try
            {
                HttpClient client = new HttpClient();
                DWHttp dW = new DWHttp();

                HttpRequestMessage req = dW.buildDefaultHttpRequestGet();

                UriBuilder uriBuilder = new UriBuilder(req.RequestUri.Scheme + "://" + req.RequestUri.Host);
                uriBuilder.Path += $"api/ConnectionSet/{env.cname}";

                req.RequestUri = uriBuilder.Uri;

                //Debug Logging >>
                logger.LogDebug($"Request URI: {req.RequestUri}");
                //Debug Logging <<

                var responseStr = await client.SendAsync(req);

                string content = await responseStr.Content.ReadAsStringAsync();

                //Debug Logging >>
                logger.LogDebug($"Response: {responseStr} {content}");
                //Debug Logging <<

                connectionSet = JsonConvert.DeserializeObject<DWConnectionSet>(content);



            }
            catch (Exception ex)
            {
                logger.LogError(ex.ToString());
            }
        }

    }


}

