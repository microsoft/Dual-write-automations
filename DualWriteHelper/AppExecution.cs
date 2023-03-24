// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Text.Json.Nodes;
using DWLibary.Struct;
using DWLibary;
using static System.Net.Mime.MediaTypeNames;
using DWLibary.Engines;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Logging;
using DWHelper;
using OpenQA.Selenium;
using OpenQA.Selenium.Edge;
using System.Diagnostics;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Hosting;
using System.Security.Cryptography;

namespace DWHelper
{
    public class AppExecution
    {
       
        ILogger logger;
        IHostApplicationLifetime lifeTime;

        public AppExecution(ILogger _logger, IHostApplicationLifetime _lifetime)
        {
            logger = _logger;
            //exportConfig = false;
            lifeTime = _lifetime;
        }

        public void run()
        {
            try
            {

                GlobalVar.init(logger);

                //string test = MFAGen.getMFAKey();

                //check if the given username is a user or a client id: 
                //ClientId won't work

                if (!GlobalVar.username.Contains("@"))
                {
                    //Client / Secret auth

                    ServicePrincipalAuth servicePrincipalAuth = new ServicePrincipalAuth(logger);
                    if(!servicePrincipalAuth.authenticate().Result)
                        return;

                }
                else
                {

                    //user based authentication

                    logger.LogInformation("Get access token, opening Edge");

                    checkEdgeVersionAndRetrieveToken();


                    

                }

                TokenRefresh tokenRefresh = new TokenRefresh(logger);
                tokenRefresh.run();

                logger.LogInformation("Get Environment");

                DWEnvCalls dWEnvCalls = new DWEnvCalls();
                DWEnvironment dwEnv = dWEnvCalls.getEnvironment().Result;

                if (dwEnv.cid == null || dwEnv.cid.Length == 0)
                {
                    logger.LogInformation("Environment is not linked, exiting");
                    lifeTime.StopApplication();
                    return;
                }


                //now do the Wiki Upload
                //DWADOWikiEngine adoWiki = new DWADOWikiEngine(dwEnv, logger);
                //adoWiki.runWikiUpload().Wait();

                logger.LogInformation($"Runmode: {GlobalVar.runMode}");

                DWMapEngine mapEngine = new DWMapEngine(dwEnv, logger);
                if (GlobalVar.runMode == DWEnums.RunMode.export)
                {
                    logger.LogInformation("Exporting config parameter is true");
                    mapEngine.generateMapConfig().Wait();
                }
                else
                {
                    if (!GlobalVar.noSolutions)
                    {
                        DWSolutionEngine dWSolution = new DWSolutionEngine(dwEnv, logger);
                        dWSolution.applySolutions().Wait();
                    }


                    mapEngine.applyMaps().Wait();


                    // now do the Wiki Upload
                    DWADOWikiEngine adoWiki = new DWADOWikiEngine(dwEnv, logger);
                    adoWiki.runWikiUpload().Wait();
                }



               




            }
            catch (Exception ex)

            {
                logger.LogError(ex.ToString());
            }
            
            lifeTime.StopApplication();
        }

        private void checkKillEdgeDriver()
        {
            foreach (Process p in Process.GetProcessesByName("msedgedriver"))
            {
                p.Kill();
            }
        }

        public void reAuthenticate()
        {

            checkEdgeVersionAndRetrieveToken();

        }

        private void checkEdgeVersionAndRetrieveToken()
        {


            checkKillEdgeDriver();
            var version = FileVersionInfo.GetVersionInfo(@"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe");

            logger.LogInformation($"Your Edge version: {version.ProductMajorPart}");

            EdgeUniversal uni = new EdgeUniversal(logger);
            uni.getToken();

            //switch(version.ProductMajorPart)
            //{
            //    case 106:
            //        EdgeMain main = new EdgeMain(logger);
            //        main.getToken();
            //        break;

            //    //case 104:
            //    //    Edge104 edge104 = new Edge104();
            //    //    edge104.getToken();
            //    //    break;

            //    //case 105:
            //    //    Edge105 edge103 = new Edge105(logger);
            //    //    edge103.getToken();
            //    //    break;


            //    //case 102:
            //    //    Edge106 edge106 = new Edge106();
            //    //    edge106.getToken();
            //    //    break;
            //    //case 101:
            //    //    Edge101 edge101 = new Edge101();
            //    //    edge101.getToken();
            //    //    break;

            //        //try run any other instance with main
            //    default:
            //        EdgeMain main2 = new EdgeMain(logger);
            //        main2.getToken();
            //        break;

            //        //outdated
            //        //case 100:
            //        //    Edge100 edge100 = new Edge100();
            //        //    edge100.getToken();
            //        //    break;
            //}

           

        }
        

    }
}
