// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.TeamFoundation.Wiki.WebApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
using System.Diagnostics.Metrics;
using System.Configuration;
using System.IO;
using Microsoft.Extensions.Logging;

namespace DWLibary
{



    public class ADOWikiUpload
    {
        
        string projectName = "";
        
        string pat = "";
        public string wikiPath = "";
        string wikiName = "";
        string orgURL = "";
        public bool useUpload = false;

        private WikiHttpClient wikiClient;

        private WikiV2 wiki;

        ILogger logger;

        public ADOWikiUpload(ILogger _logger)
        {
            logger = _logger;
            init().Wait();
        }


        private async Task init()
        {


            foreach(ADOWikiParameter param in GlobalVar.dwSettings.ADOWikiParameters)
            {
                switch(param.Key.ToUpper())
                {
                    case "USEADOWIKIUPLOAD":
                        if (GlobalVar.useadowikiupload)
                            useUpload = GlobalVar.useadowikiupload;
                        else
                            useUpload = Convert.ToBoolean(param.Value);

                        break;

                    case "ACCESSTOKEN":
                        if (GlobalVar.adotoken != String.Empty)
                            pat = GlobalVar.adotoken;
                        else
                            pat = param.Value;
                        break;

                    case "PROJECTNAME":
                        projectName = param.Value;
                        break;

                    case "ORGANIZATIONURL":
                        orgURL = param.Value;
                        break;

                    case "WIKINAME":
                        wikiName = param.Value;
                        break;

                    case "WIKIPATH":
                        wikiPath = param.Value;
                        break;

                }
            }

            if (!useUpload)
                return;

            var creds = new VssBasicCredential(string.Empty, pat);

            Uri baseUrl = new Uri(orgURL);

            wikiClient = new WikiHttpClient(baseUrl, creds);

            try
            {
                wiki = await wikiClient.GetWikiAsync(projectName, wikiName);
            }
            catch (Exception ex)
            {
                logger.LogError("Could not authenticate to the wiki, please check the configuration");
                useUpload= false;
            }


        }

        public string CombineForward(string path1, string path2)
        {

            string ret = string.Empty;


            if(path1.Length > 0)
            {
                if (path1.Substring(0, 1) != "/")
                    path1 = "/" + path1;
            }

            if(path2.Length > 0)
            {
                if(path2.Substring(path2.Length-1, 1) == "/")
                    path2 = path2.Substring(0, path2.Length-1);

            }

            if(path1.Length > 0 && path2.Length > 0)
            {

                string endPath1 = path1.Substring(path1.Length - 1, 1);
                string beginPath2 = path2.Substring(0, 1);

                if (endPath1 != "/" && beginPath2 == "/")
                    ret = path1 + path2;

                if (endPath1 == "/" && beginPath2 != "/")
                    ret = path1 + path2;

                if (endPath1 != "/" && beginPath2 != "/")
                    ret = path1 + "/" +  path2;

                if (endPath1 == "/" && beginPath2 == "/")
                    ret = path1 + path2.Substring(1, path2.Length - 1);

            }


            return ret;

        }

        public async Task createUpdatePage(string content, string path, bool updatePage = true)
        {

            try
            {
                if (!useUpload)
                    return;


                string finalPath = CombineForward(wikiPath, path);

                content = $"_This Page is automatically generated, if you do makes changes it may be overwritten_{Environment.NewLine}" + content;

                WikiPageCreateOrUpdateParameters parameters = new WikiPageCreateOrUpdateParameters();
                parameters.Content = content;



                WikiPageResponse wikiPage = null;
                //parameters.
                try
                {
                    wikiPage = await wikiClient.GetPageAsync(wiki.ProjectId, wiki.Id, finalPath);
                }
                catch { }


               

                if (wikiPage != null)
                {
                    if (updatePage)
                        await wikiClient.CreateOrUpdatePageAsync(parameters, wiki.ProjectId, wiki.Id, finalPath, wikiPage.ETag.FirstOrDefault());
                }
                else

                {
                    await wikiClient.CreateOrUpdatePageAsync(parameters, wiki.ProjectId, wiki.Id, finalPath, "");
                }

            }
            catch(Exception ex)
            {
                logger.LogError(ex.ToString());
            }

        }

        public async Task runTest()
        {

           await  init();

            if (!useUpload)
                return;

            string finalPath = "/DualWrite Automation/something";
            //var wikis =  await wiki.GetAllWikisAsync(); //Gets all Wikis 


            // Get data about a specific repository
            //  var repo = gitClient.GetRepositoryAsync(projectName, repoName).Result;

            WikiPageCreateOrUpdateParameters parameters = new WikiPageCreateOrUpdateParameters();
            parameters.Content = "Some content3";


            //parameters.

            WikiPageResponse wikiPage = null;
            //parameters.
            try
            {
                wikiPage = await wikiClient.GetPageAsync(wiki.ProjectId, wiki.Id, finalPath);
            }
            catch { }



            await wikiClient.CreateOrUpdatePageAsync(parameters, wiki.ProjectId, wiki.Id, finalPath, wikiPage == null ? "" : wikiPage.ETag.FirstOrDefault());


        }
    }
}
