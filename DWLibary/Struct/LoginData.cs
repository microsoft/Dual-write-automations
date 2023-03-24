// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DWLibary.Struct
{
    public class LoginData
    {
        public string token_type { get; set; }
        public string scope { get; set; }
        public int expires_in { get; set; }
        public int ext_expires_in { get; set; }

        private string _access_token;
        public string access_token { 
            get { 
                return _access_token; 
            } 
            set {
                _access_token = value; 
                getUsername(); 
            } 
        }
        public string refresh_token { get; set; }
        public string id_token { get; set; }
        public string client_info { get; set; }
        public DateTime tokenRefreshDate { get; set; }

        private AuthenticationResult _authResult;
        public AuthenticationResult authResult { get
            {
                return _authResult;

            } set
            {
                _authResult = value;
                if(value != null)
                    _access_token = value.AccessToken;
                //tokenRefreshDate = value.

            }
        }

        public string username { get; set; }

        public LoginData()
        {
            tokenRefreshDate = DateTime.Now;  
        }

        public string getUsername()
        {
            string ret = string.Empty;

            try

            {

                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadToken(access_token);
                var tokenS = jsonToken as JwtSecurityToken;

                ret = tokenS.Claims.First(claim => claim.Type == "upn").Value;

                username = ret;
            }
            catch { }

            return ret;
        }
    }
}
