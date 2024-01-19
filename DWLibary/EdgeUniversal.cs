// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenQA.Selenium.DevTools.V120.Network;
using DevToolsSessionDomains = OpenQA.Selenium.DevTools.V120.DevToolsSessionDomains;
using DWLibary;
using OpenQA.Selenium;
using OpenQA.Selenium.DevTools;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Remote;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using DWLibary.Struct;
using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Runtime.InteropServices;


namespace DWLibary
{
    public class EdgeUniversal
    {
        protected static EdgeDriver driver;
        NetworkAdapter networkAdapter;

        protected DevToolsSession session;
        protected EdgeDriverService service;
        protected static ILogger logger;

        public EdgeUniversal(ILogger _logger)
        {
            logger = _logger;
        }

        protected virtual void initDriverService()
        {
            //Drivers found here: https://www.selenium.dev/downloads/
            
            service = OpenQA.Selenium.Edge.EdgeDriverService.CreateDefaultService(Path.Combine(GlobalVar.curExecutingDirectory(), "Drivers"), @"msedgedriver.exe");
            //disable console logging 
            service.UseVerboseLogging = false;
            service.EnableVerboseLogging = false;
            service.Start();
        }

        private void checkKillEdgeDriver()
        {
            foreach (Process p in Process.GetProcessesByName("msedgedriver"))
            {
                p.Kill();
            }
        }

        protected virtual void initNetworkAdapter()
        {
            networkAdapter = new NetworkAdapter(session);
            networkAdapter.Enable(new EnableCommandSettings()).GetAwaiter().GetResult();
            networkAdapter.ResponseReceived += NetworkAdapter_ResponseReceived;
        }

        private bool useClientAuthentication()
        {
            if(GlobalVar.parsedOptions.tenant != String.Empty || GlobalVar.parsedOptions.clientId != String.Empty)
            {
                return true;
            }

            return false;
        }

        public void getToken()
        {

            if(useClientAuthentication())
            {
                ServicePrincipalAuth principalAuth = new ServicePrincipalAuth(logger);
                principalAuth.authenticate().Wait();
                return;
            }

            checkKillEdgeDriver();
            var version = FileVersionInfo.GetVersionInfo(@"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe");

            logger.LogInformation($"Your Edge version: {version.ProductMajorPart}");


            //if token was retrieved from the local files dont run selenium
            if (GlobalVar.loginData == null
                || GlobalVar.loginData.access_token == null
                || GlobalVar.loginData.access_token == String.Empty
                || GlobalVar.baseUrl == null
                || GlobalVar.baseUrl == String.Empty)
            {

                //downloads the matching Edge Driver
                DownloadEdgeDriver();

                initDriverService();

                var options = new OpenQA.Selenium.Edge.EdgeOptions();

                string localFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                localFolder = Path.Combine(localFolder, "Microsoft\\Edge\\User Data");

                if(GlobalVar.parsedOptions.notinprivate == null ||  !GlobalVar.parsedOptions.notinprivate)
                    options.AddArguments("-inprivate");

                driver = new EdgeDriver(service, options);
               
                
                session = ((IDevTools)driver).GetDevToolsSession();
                
                initNetworkAdapter();


                UriBuilder builder = new UriBuilder(GlobalVar.dataintegratorURL);
                builder.Path = $"dualWrite";
                builder.Query = $"axenv={GlobalVar.foEnv}";

                driver.Navigate().GoToUrl(builder.Uri.AbsoluteUri);

                bool userOk = false, pwOk = false, MFAOk = false;

                DateTime timeoutDT = DateTime.Now;

                while (GlobalVar.loginData is null || GlobalVar.loginData.access_token is null || GlobalVar.baseUrl is null || GlobalVar.baseUrl == String.Empty)
                {
                    Thread.Sleep(1000);


                    //if username given

                    if (GlobalVar.username != String.Empty)
                    {


                        try
                        {

                            if (!userOk)
                            {
                                logger.LogInformation("Entering username");
                                driver.FindElement(By.Name("loginfmt")).SendKeys(GlobalVar.username); //Username
                                userOk = true;
                                driver.FindElement(By.Id("idSIButton9")).Submit(); //passwd
                                Thread.Sleep(2000);
                            }

                            if (!pwOk && GlobalVar.password != String.Empty)
                            {
                                logger.LogInformation("Entering password");
                                driver.FindElement(By.Name("passwd")).SendKeys(GlobalVar.password); //Enter PW

                                driver.FindElement(By.Id("idSIButton9")).Submit(); //Confirm PW
                                pwOk = true;
                                Thread.Sleep(2000);
                            }

                            //MFA coverage
                            try
                            {

                                if (!MFAOk && GlobalVar.config.AppSettings.Settings["MFASecretKey"].Value != "")
                                {
                                    
                                    IWebElement element = driver.FindElement(By.Id("idTxtBx_SAOTCC_OTC"));

                                    logger.LogInformation("MFA is enabled!");
                                    logger.LogInformation("Entering MFA Key");
                                    element.SendKeys(MFAGen.getMFAKey());
                                    MFAOk = true;
                                    
                                    driver.FindElement(By.Id("idSubmit_SAOTCC_Continue")).Submit(); //Confirm MFA Code
                                    Thread.Sleep(2000);

                                }

                            }
                            //if not found it throws 
                            catch { }

                            if (GlobalVar.password != String.Empty && GlobalVar.username != String.Empty)
                                driver.FindElement(By.Id("idSIButton9")).Submit(); //Confirm Stay Signed in
                        }
                        catch
                        {

                        }

                    }
                    else
                    {
                        logger.LogInformation("No username / password given - waiting manual entry!");
                    }

                    if(DateTime.Now - timeoutDT > TimeSpan.FromMinutes(1))
                    {
                        GlobalVar.addError("Authentication failed - could not login with credentials");
                        throw new Exception("Authentication failed!");

                    }


                }

                //get the Gateway URL
                while (GlobalVar.baseUrl == null || GlobalVar.baseUrl.Length == 0)
                {
                    if (DateTime.Now - timeoutDT > TimeSpan.FromMinutes(3))
                    {
                        throw new Exception("Authentication failed, check username, password and the environment url");

                    }
                    Thread.Sleep(1000);
                }

                //nothing to do here anymore 
                driver.Close();
            }
        }

        public void DownloadEdgeDriver()
        {
            

            
           // string execFolder = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string driverfolder = Path.Combine(GlobalVar.curExecutingDirectory(),"Drivers");
            string edgeVersionFile = Path.Combine(driverfolder,"edgeversion.txt");
           // string tmpfolder = "Drivers/";
            FileVersionInfo info = null;
            string version = null;
            string drivername = null;

            logger.LogInformation("Matching edge version with edge driver");
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (!File.Exists(edgeVersionFile))
                    File.Create(edgeVersionFile).Close();

                if (Environment.Is64BitOperatingSystem is true)
                {
                    info = FileVersionInfo.GetVersionInfo(@"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe");
                    drivername = "edgedriver_win64.zip";
                }
                else
                {
                    info = FileVersionInfo.GetVersionInfo(@"C:\Program Files\Microsoft\Edge\Application\msedge.exe");
                    drivername = "edgedriver_win32.zip";
                }

                version = info.FileVersion;
                string current = File.ReadAllText(edgeVersionFile).ToString();
                if (version != current)
                {

                    string edgeDriverPath = Path.Combine(driverfolder, "msedgedriver.exe");
                    logger.LogInformation($"Current version {current}, Edge Version {version} - downloading matching driver...");

                    if (File.Exists(Path.Combine(driverfolder, "*.zip")))
                        File.Delete(Path.Combine(driverfolder, "*.zip"));
                    if (File.Exists(edgeDriverPath))
                        File.Delete(edgeDriverPath);

                    using (var client = new WebClient())
                    {
                        client.DownloadFile("https://msedgedriver.azureedge.net/" + version + "/" + drivername, driverfolder + "\\" + drivername);
                    }
                    if (File.Exists(edgeDriverPath))
                        File.Delete(edgeDriverPath);
                    if (Directory.Exists(Path.Combine(driverfolder, "Driver_Notes")))
                        Directory.Delete(Path.Combine(driverfolder, "Driver_Notes"), true);


                    ZipFile.ExtractToDirectory(Path.Combine(driverfolder, drivername), driverfolder);
                    File.WriteAllText(edgeVersionFile, version);

                    //Delet zip file
                    File.Delete(Path.Combine(driverfolder, drivername));

                    logger.LogInformation("Edge driver download completed.");
                }
            }
        }

        internal static void processNetworkTokenResponse(string _body)
        {
            var json = JsonConvert.DeserializeObject<LoginData>(_body);

            GlobalVar.loginData = json;


        }

        internal static void processGateWayURL(string _host)
        {
            if (GlobalVar.baseUrl != String.Empty)
                return;
            //Try to get the gateway URL.
            try
            {


                UriBuilder uriBuilder = new UriBuilder(_host);

                GlobalVar.baseUrl = uriBuilder.Scheme + "://" + uriBuilder.Host;



            }
            catch (Exception ex)
            {
                logger.LogError(ex.ToString());
            }
        }

        private static EventHandler<ResponseReceivedEventArgs> NetworkAdapter_ResponseReceived = async (sender, e) =>
        {

            NetworkAdapter adapter = sender as NetworkAdapter;

            if (e.Type == ResourceType.Fetch && e.Response.Url == "https://login.microsoftonline.com/common/oauth2/v2.0/token")
            {
                string whatever = String.Empty;
                //Network mw = new Network();
                //var test = netWork.get

                var response = await adapter.Session.GetVersionSpecificDomains<DevToolsSessionDomains>().Network.GetResponseBody(new GetResponseBodyCommandSettings() { RequestId = e.RequestId });

                processNetworkTokenResponse(response.Body);
            }

            if (e.Type == ResourceType.Fetch && e.Response.Url.Contains("Version") && e.Response.Url.Contains("projectmanagementservice"))
            {

                processGateWayURL(e.Response.Url);

            }


        };
    }
}
