// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using DWLibary.Struct;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DWLibary
{
    public class DWHttp
    {
        
        HttpRequestMessage _httpRequest;
        DWEnvironment env;

        private const string CustomUserAgent = "DualWriteHelper/1.0 (Windows NT 10.0; Win64; x64)";

        public DWHttp(DWEnvironment _env = default)
        {
            env = _env;
        }


        public HttpRequestMessage buildDefaultHttpRequestPost()
        {
            _httpRequest = new HttpRequestMessage();
            

            _httpRequest.Method = HttpMethod.Post;
            _httpRequest.Headers.Add("Accept", "application/json");
            _httpRequest.Headers.Add("Origin", GlobalVar.dataintegratorURL.AbsoluteUri);
            _httpRequest.Headers.Add("User-Agent", CustomUserAgent);

            _httpRequest.RequestUri = buildReqUri();
            buildAuth();


            return _httpRequest;
        }

        private Uri buildReqUri()
        {
            Uri ret = null;

            if (GlobalVar.baseUrl != null && GlobalVar.baseUrl.Length > 0)
            {
                UriBuilder uriBuilder = new UriBuilder(GlobalVar.baseUrl);
                uriBuilder.Path = "/api/DualWriteManagement/1.0/";

                ret = uriBuilder.Uri;
            }

            return ret;
        }

        public HttpRequestMessage buildDefaultHttpRequestGet()
        {
            try
            {
                _httpRequest = new HttpRequestMessage();

                _httpRequest.Method = HttpMethod.Get;

                _httpRequest.RequestUri = buildReqUri();
           
                buildAuth();
            }
            catch (Exception ex)
            {

            }


            return _httpRequest;
        }

        private void buildAuth()
        {

            LoginData localLogin = null;

            if(env.foEnvironment != null)
            {
                localLogin = GlobalVar.savedTokens.Where(x => x.environment.ToUpper().Equals(env.foEnvironment.ToUpper())).FirstOrDefault();
            }

            _httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", localLogin != null ? localLogin.access_token : GlobalVar.loginData.access_token);
        }
        


    }
}
