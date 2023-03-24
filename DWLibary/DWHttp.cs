// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

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
    

        public HttpRequestMessage buildDefaultHttpRequestPost()
        {
            _httpRequest = new HttpRequestMessage();
            

            _httpRequest.Method = HttpMethod.Post;
            _httpRequest.Headers.Add("Accept", "application/json");
            _httpRequest.Headers.Add("Origin", GlobalVar.dataintegratorURL.AbsoluteUri);

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
            _httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", GlobalVar.loginData.access_token);
        }
        


    }
}
