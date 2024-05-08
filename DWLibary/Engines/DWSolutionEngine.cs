// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using DWLibary.Struct;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace DWLibary.Engines
{
    public class DWSolutionEngine
    {
        DWEnvironment env;
        Solutions solutions;
        List<SolutionApplyObj> solutionRequests;
        SolutionRequestResponse response;
        ILogger logger;

        public DWSolutionEngine(DWEnvironment _env, ILogger _logger)
        {
            env = _env;
            logger = _logger;

        }

        public async Task applySolutions()
        {
            solutions = GlobalVar.dwSettings.Solutions;


            buildSolutionRequest();

            await postSolutionApply();

            

        }

        private async Task<bool> checkSolutionApplied()
        {
            bool ret = false;

            try
            {
                HttpClient client = new HttpClientWithRetry();
                DWHttp dW = new DWHttp();

                HttpRequestMessage req = dW.buildDefaultHttpRequestGet();

                UriBuilder uriBuilder = new UriBuilder(req.RequestUri);
                uriBuilder.Path += $"SolutionAware/{env.cid}/Status/{response.requestId}";
                req.RequestUri = uriBuilder.Uri;

                //Debug Logging >>
                logger.LogDebug($"Request URI: {req.RequestUri}");
                //Debug Logging <<

                var responseStr = await client.SendAsync(req);

                string content = await responseStr.Content.ReadAsStringAsync();

                //Debug Logging >>
                logger.LogDebug($"Response: {responseStr} {content}");
                //Debug Logging <<


                SolutionResult result = JsonConvert.DeserializeObject<SolutionResult>(content);


                if (result.state == "2")
                {
                    ret = true;
                    logger.LogInformation($"Successfully applied solutions");
                }

                if(result.state == "3")
                {
                    //error state
                    logger.LogError("Solution applying errored");
                    ret = true;
                }


            }
            catch (Exception ex)
            {

            }

            return ret;
        }


        private async Task postSolutionApply()
        {

            //each solution should be applied seperatly otherwise duplicates will appear 

            foreach (SolutionApplyObj solutionReq in solutionRequests)
            {
                logger.LogInformation($"Applying solution {solutionReq.solutions[0].criteria.uniquename}");


                HttpClient client = new HttpClientWithRetry();
                DWHttp dW = new DWHttp();

                HttpRequestMessage req = dW.buildDefaultHttpRequestPost();

                UriBuilder uriBuilder = new UriBuilder(req.RequestUri);
                uriBuilder.Path += $"SolutionAware/{env.cid}/RestoreTemplates";
                req.RequestUri = uriBuilder.Uri;
                req.Content = new StringContent(JsonConvert.SerializeObject(solutionReq), Encoding.UTF8, "application/json");

                //Debug Logging >>
                logger.LogDebug($"Request URI: {req.RequestUri}");
                //Debug Logging <<

                var responseStr = await client.SendAsync(req);

                string content = await responseStr.Content.ReadAsStringAsync();

                //Debug Logging >>
                logger.LogDebug($"Response: {responseStr} {content}");
                //Debug Logging <<

                response = JsonConvert.DeserializeObject<SolutionRequestResponse>(content);


                while (!await checkSolutionApplied())
                {
                    //wait until successful
                    logger.LogInformation($"Waiting for solutions to be applied.");
                    Thread.Sleep(1000);
                }


            }


        }


        private void buildSolutionRequest()
        {

            solutionRequests = new List<SolutionApplyObj>();
            foreach (Solution solution in solutions)
            {
                SolutionApplyObj solutionRequest = new SolutionApplyObj();
                solutionRequest.action = "2"; //Apply
                solutionRequest.solutions = new List<SolutionCriteria>();

                SolutionCriteria criteria = new SolutionCriteria();
                SolutionCriteriaValue solutionCriteriaValue = new SolutionCriteriaValue();
                //todo
                solutionCriteriaValue.uniquename = solution.Name;

                criteria.criteria = solutionCriteriaValue;
                solutionRequest.solutions.Add(criteria);

                solutionRequests.Add(solutionRequest);
            }

            logger.LogInformation($"Solution request build: {JsonConvert.SerializeObject(solutionRequests)}");
        }

    }
}
