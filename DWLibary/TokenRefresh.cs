// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using DWLibary.Struct;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace DWLibary
{
    public class TokenRefresh
    {
        ILogger logger;

        public TokenRefresh(ILogger _logger)
        {
            logger = _logger;
        }

        public void run()
        {

            Thread thread = new Thread(new ThreadStart(runThread));
            thread.Start();

        }


        public bool tryGetRefreshToken(bool runRefreshThread = false)
        {
            bool ret = false;

            TimeSpan ts = GlobalVar.loginData.tokenRefreshDate.AddSeconds(GlobalVar.loginData.expires_in) - DateTime.Now;

            //token is expired
            if (ts.TotalMilliseconds < 0)
            {

                if (getRefreshToken().Result)
                {
                    ret = true;

                    if (runRefreshThread)
                        run();
                }
                else
                {
                    //reset the variable
                    GlobalVar.loginData = new LoginData();
                }

            }
            //still valid
            else
            {
                if (runRefreshThread)
                    run();
            }

            return ret;

        }

        private async void runThread()
        {

            while(true)
            {
                TimeSpan ts = GlobalVar.loginData.tokenRefreshDate.AddSeconds(GlobalVar.loginData.expires_in) - DateTime.Now;

                int ms = (int)ts.TotalMilliseconds - (60 * 1000 * 5);

                if(ms > 0)
                    Thread.Sleep(ms); // 5 mins deduction

                if(GlobalVar.loginData.authResult != null)
                {
                    ServicePrincipalAuth servicePrincipalAuth = new ServicePrincipalAuth(logger);
                    await servicePrincipalAuth.authenticate();
                }
                else
                {
                    await getRefreshToken();
                }

                


            }

        }


        public async Task<bool> getRefreshToken()
        {
            bool ret = false;

            LoginData refresh = await getLoginDataRefreshed();

            if(refresh != null)
            {
                ret = true;
                GlobalVar.loginData = refresh;
            }

            

            return ret;
        }

        public async Task<LoginData> getLoginDataRefreshed(LoginData _loginData = null, ILogger _log = null)
        {

            LoginData ret = null;

            HttpClient client = new HttpClient();
            HttpRequestMessage req = new HttpRequestMessage();

            req.Method = HttpMethod.Post;
            req.Headers.Add("Accept", "application/json");
            req.Headers.Add("origin", GlobalVar.dataintegratorURL.AbsoluteUri);

            UriBuilder builder = new UriBuilder(GlobalVar.dataintegratorURL);
            builder.Path = "dualWrite";

            var formData = new[]
            {
                new KeyValuePair<string, string>("client_id", "2e49aa60-1bd3-43b6-8ab6-03ada3d9f08b"),
                new KeyValuePair<string, string>("scope", "https://IntegratorApp.com/.default openid profile offline_access"),
                new KeyValuePair<string, string>("redirect_uri", builder.Uri.AbsoluteUri),
                new KeyValuePair<string, string>("grant_type", "refresh_token"),
                new KeyValuePair<string, string>("refresh_token", _loginData == null ? GlobalVar.loginData.refresh_token : _loginData.refresh_token),
            };

            req.Content = new FormUrlEncodedContent(formData);

            req.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
            req.Content.Headers.ContentType.CharSet = "UTF-8";

            req.RequestUri = new Uri("https://login.microsoftonline.com/common/oauth2/v2.0/token");

            var response = await client.SendAsync(req);

            if(response.IsSuccessStatusCode)
            {
                if (_log != null)
                    _log.LogInformation("Token retrieved successful");

                ret = JsonConvert.DeserializeObject<LoginData>(await response.Content.ReadAsStringAsync());

            }
            else
            {
                string error = await response.Content.ReadAsStringAsync();

                if (_log != null)
                    _log.LogError(error);
            }

            return ret;
        }
    }
}
