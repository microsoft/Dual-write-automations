// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using DWLibary.Struct;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace DWLibary
{
    public static class GlobalVar
    {
        private static LoginData _loginData;

        public static int maxThreads { get; set; }

        //public static bool exportConfig { get; set; }
        public static DWEnums.MapStatus exportState { get; set; }
        public static DWEnums.ExportOptions exportOption { get; set; }

        public static DWEnums.RunMode runMode { get; set; }
        public static DWEnums.ExecutionMode executionMode { get; set; }

        public static DWSettings dwSettings { get; private set; }

        public static Configuration config { get; private set; }

        public static string configFileName { get; set; }

        public static string tenant { get; set; }

        public static bool noSolutions { get; set; }
        public static string mfasecret { get; set; }
        public static bool useadowikiupload { get; set; }
        public static string adotoken { get; set; }

        public static Uri dataintegratorURL { get; set; }

        public static List<ErrorMessage> errors { get; set; }

        public static LoginData loginData
        {
            get
            {
                return _loginData;
            }
            set
            {
                _loginData = value;
                saveLoginData();
            }

        }

        public static string username { get; set; }
        public static string password { get; set; }

        private static string gatewayUrl { get; set; }
        public static string baseUrl
        {
            get
            {
                return gatewayUrl;
            }
            set
            {
                gatewayUrl = value;
                saveGateway();
            }
        }

        public static string foEnv { get; set; }


        public static List<LoginData> savedTokens { get; set; }

        public static List<EnvGatewayCombination> envGateways { get; set; }

        public static void initTestValues()
        {
            baseUrl = "https://projectmanagementservice.weu-il107.gateway.prod.island.powerapps.com";///api/DualWriteManagement/1.0/";
        }

        public static void initConfig()
        {
            var map = new ExeConfigurationFileMap();

            configFileName = Path.Combine(curExecutingDirectory(), configFileName);
            if(logger != null)
                logger.LogInformation($"Config path: {configFileName}");
            if (configFileName != String.Empty &&  File.Exists(configFileName))
            {
                map.ExeConfigFilename = configFileName;
                config = ConfigurationManager.OpenMappedExeConfiguration(map, ConfigurationUserLevel.None);
                if (logger != null)
                    logger.LogInformation("Custom configuration loaded");
            }
            else
            {
                config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                if (logger != null)
                    logger.LogInformation("Default configuration loaded");
            }

            dwSettings = config.GetSection("DWSettings") as DWSettings;

            foreach(MapConfig mc in dwSettings.MapConfigs)
            {
                mc.initSettings();

            }

        }

        private static ILogger logger;
        //used to display all errors at the end

        public static void addError(string _errorMessage, string prefix = "")
        {

            ErrorMessage message = new ErrorMessage();
            message.error = _errorMessage;
            message.prefix = prefix;


            errors.Add(message);

            if(prefix != "")
                logger.LogError($"{prefix}: {_errorMessage}");
            else
                logger.LogError(_errorMessage);
        }

        public static void outputErrors()
        {

            errors = errors.OrderBy(x => x.prefix).ToList();

            logger.LogInformation("Output all errors..");
            foreach(ErrorMessage e in errors)
            {
                logger.LogError($"{e.prefix}: {e.error}");
            }

        }

        public static string curExecutingDirectory()
        {
            string ret = String.Empty;

            try
            {
                var dir = new DirectoryInfo(System.Reflection.Assembly.GetExecutingAssembly().Location);
                ret = dir.Parent.FullName;

            }
            catch(Exception ex) { }


            return ret;
        }

        public static void setdataintegratorURL()
        {

            //UriBuilder builder = new UriBuilder();

            string configURL = "https://dataintegrator.trafficmanager.net";

            try
            {
                configURL = config.AppSettings.Settings["dataintegratorURL"].Value;
            }
            catch
            {
               
            }
            dataintegratorURL = new UriBuilder(configURL).Uri;
        }

        public static void init(ILogger _logger)
        {
            
            logger = _logger;
            initConfig();

            setdataintegratorURL();

            errors = new List<ErrorMessage>();

            loginData = new LoginData();
#pragma warning disable CS8601 // Possible null reference assignment.
            //not needed anymore, parsing automatically - needed if using client id/ secret

            if (baseUrl == null)
                baseUrl = String.Empty;

            //if coming from commandline args dont use the config file
            if (foEnv == null ||foEnv == String.Empty)
                foEnv = config.AppSettings.Settings["FOEnvironment"].Value;
#pragma warning restore CS8601 // Possible null reference assignment.

            maxThreads = Convert.ToInt16(config.AppSettings.Settings["maxThreads"].Value);
            executionMode = DWEnums.GetValueFromDescription<DWEnums.ExecutionMode>(config.AppSettings.Settings["executionMode"].Value);

            if(runMode == DWEnums.RunMode.none)
                runMode = DWEnums.GetValueFromDescription<DWEnums.RunMode>(config.AppSettings.Settings["runMode"].Value);

            if (maxThreads == 1)
                executionMode = DWEnums.ExecutionMode.sequential;

            readLoginTokens();

            if (savedTokens == null)
                savedTokens = new List<LoginData>();
            else
            {
                //check if tokes exist
                if (GlobalVar.username != null && GlobalVar.username != String.Empty)
                {
                    GlobalVar.loginData = savedTokens.Where(x => x.username.ToUpper().Equals(GlobalVar.username.ToUpper())).FirstOrDefault();

                    if (GlobalVar.loginData != null)
                    {
                        TokenRefresh tr = new TokenRefresh(_logger);
                        tr.tryGetRefreshToken(true);
                    }
                    else
                        loginData = new LoginData();

                }
            }

            readGateways();
            if (envGateways != null)
            {
                EnvGatewayCombination lookup = envGateways.Where(x => x.environment.Equals(foEnv)).FirstOrDefault();

                if (lookup != null)
                    baseUrl = lookup.gateway;

            }
            else
            {
                envGateways = new List<EnvGatewayCombination>();
            }

           // envGateways

        }



        private static void saveGateway()
        {
            if (baseUrl == null || baseUrl == String.Empty || envGateways == null)
                return;

            EnvGatewayCombination comb = new EnvGatewayCombination();
            comb.environment = foEnv;
            comb.gateway = baseUrl;

            EnvGatewayCombination lookup = envGateways.Where(x => x.environment.Equals(comb.environment)).FirstOrDefault();

            if (lookup != null)
            {
                envGateways.Remove(lookup);
            }

            envGateways.Add(comb);
            writeGateways();


        }

        private static void saveLoginData()
        {

            //dont want to have orphant records
            if (loginData == null || loginData.access_token == null || loginData.access_token == String.Empty)
                return;


            LoginData lookup = savedTokens.Where(x => x.username.ToUpper().Equals(GlobalVar.loginData.username.ToUpper())).FirstOrDefault();

            if(lookup != null)
            {
                savedTokens.Remove(lookup);
            }

            loginData.environment = foEnv;

            savedTokens.Add(GlobalVar.loginData);
            writeLoginTokens();


        }

        public static void writeGateways()
        {
            try
            {

                var base64 = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(envGateways));

                using (StreamWriter sw = new StreamWriter(@"envgateways.txt"))
                {
                    sw.WriteLine(Convert.ToBase64String(base64));
                }
            }
            catch (Exception ex)
            {

            }
        }

        public static void readGateways()
        {
            try
            {



                using (StreamReader sr = new StreamReader(@"envgateways.txt"))
                {
                    var bytes = Convert.FromBase64String(sr.ReadToEnd());

                    var decodedString = Encoding.UTF8.GetString(bytes);

                    envGateways = JsonConvert.DeserializeObject<List<EnvGatewayCombination>>(decodedString);
                }
            }
            catch (Exception ex)
            {

            }
        }

        public static void writeLoginTokens()
        {
            try
            {

                var base64 = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(savedTokens));

                using (StreamWriter sw = new StreamWriter(@"tokens.txt"))
                {
                    sw.WriteLine(Convert.ToBase64String(base64));
                }
            }
            catch(Exception ex)
            {

            }
        }

        public static void readLoginTokens()
        {
            try
            {

               

                using (StreamReader sr = new StreamReader(@"tokens.txt"))
                {
                    var bytes = Convert.FromBase64String(sr.ReadToEnd());

                    var decodedString = Encoding.UTF8.GetString(bytes);

                    savedTokens = JsonConvert.DeserializeObject<List<LoginData>>(decodedString);
                }
            }
            catch (Exception ex)
            {

            }
        }

    }

    public class ErrorMessage
    {
        public string prefix { get; set; }
        public string error { get; set; }
    }
}
