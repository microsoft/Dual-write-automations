// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using DWLibary.Struct;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using static Azure.Core.HttpHeader;

namespace DWLibary.Engines
{
    public class DWADOWikiEngine
    {

        ADOWikiUpload wikiUpload;
        ILogger logger;
        DWEnvironment env;
        DWCommonEngine common;
        MapConfigs mapConfigs;
        MapConfig curMapConfig;
        DWMap currentMap;
        List<DWWikiOverview> wikiOverviewList;
        DWWikiOverview curWikiOverview;
        string path;
        string valueMapContent;

        public DWADOWikiEngine(DWEnvironment _env, ILogger _logger)
        {
            env = _env;

            logger = _logger;
            wikiOverviewList = new List<DWWikiOverview>();
            common = new DWCommonEngine(_env, _logger);
            valueMapContent = String.Empty;
        }


        public async Task runWikiUpload(bool forceUpload = false)
        {
            wikiUpload = new ADOWikiUpload(logger);

            if(forceUpload)
            {
                wikiUpload.useUpload = true;
            }

            if (!wikiUpload.useUpload)
            {
                logger.LogInformation("ADO Wikiupload won't be executed!");
                return;
            }

            logger.LogInformation($"Creating ADO Wiki...");

            path = "/DualWrite map configuration";
            //create the main Page structure
            await checkCreateMainPage();

            await runMapUpload();

            logger.LogInformation($"Completed creating ADO WiKi");

        }


        private async Task runMapUpload()
        {

            List<DWMap> dwMaps = await common.getDWMaps();

            mapConfigs = GlobalVar.dwSettings.MapConfigs;

            //make sure the overview map is present as sub pages are created for it.
            await checkCreateOverviewPage();

            List<MapConfig> reversedList = mapConfigs.Cast<MapConfig>().ToList();
            reversedList.Reverse();

            foreach (MapConfig config in reversedList)
            {
                curMapConfig = config;
                currentMap = dwMaps.Where(x => x.detail.tName.Equals(config.mapName)).FirstOrDefault();
                valueMapContent = String.Empty;

                if (currentMap.detail.pid == null)
                    continue;

                logger.LogInformation($"Starting creating page for {currentMap.detail.tName}");

                curWikiOverview = new DWWikiOverview();

                try
                {

                    await createIndividualPageUpload();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex.ToString());
                }

                logger.LogInformation($"Completed page for {currentMap.detail.tName}");

                wikiOverviewList.Add(curWikiOverview);
            }

            //create the overview page
            logger.LogInformation($"Starting creating overview page");
            await createOverViewPage();
            logger.LogInformation($"Completed overview page");

        }

        private async Task createIndividualPageUpload()
        {
            string pageName = $"{currentMap.leftEntity.displayName} - {currentMap.rightEntity.displayName} - CURRENT";

            string content = String.Empty;

            await common.getFieldMappingForMaps(currentMap);

            //headers
            content += $"## Map details {Environment.NewLine}{Environment.NewLine}";
            content += $"| **Details** |  | {Environment.NewLine}";
            content += $"|--|--|{Environment.NewLine}";

            //{Environment.NewLine}
            string versionStr = $"{currentMap.detail.template.version.major}.{currentMap.detail.template.version.minor}.{currentMap.detail.template.version.revision}.{currentMap.detail.template.version.build}";


            content += $"| **FO Entity** | {getFOEntity()} | {Environment.NewLine}";
            content += $"| **CE Table** | {getCEEntity()} | {Environment.NewLine}";
            content += $"| **Version** | {versionStr} | {Environment.NewLine}";
            content += $"| **Publisher** | {currentMap.detail.template.author}{Environment.NewLine}";
            content += $"| **Description** |{currentMap.detail.template.description} | {Environment.NewLine}";
            content += $"| **Direction** | {DWEnums.DescriptionAttr<DWEnums.DWSyncDirection>(getSyncDirection())} {Environment.NewLine}";
            content += $" **Integration key** | {await common.getCurrentKeys(currentMap)}{Environment.NewLine}";
            content += $"| **FO Filter** | {getSourceFilter()} |{Environment.NewLine}";
            content += $"| **CE Filter** | {getDestinationFilter()} |{Environment.NewLine}";

            content += $"{Environment.NewLine}";

            content += $"## Field Mappings {Environment.NewLine}{Environment.NewLine}";
            content += $"| FO Column | Direction | CE Column | FO type | CE type | Value Map | Default value {Environment.NewLine}";
            content += $"|--|--|--|--|--|--|--|{Environment.NewLine}";

            
            //get field mappings

            foreach(var data in common.curFieldMapping.entityMappingTasks[0].legs[0].fieldMappings)
            {
                content += $"| {data.sourceField} | {DWEnums.DescriptionAttr<DWEnums.DWSyncDirection>(data.syncDirection)} | {data.destinationField} | {getFOFieldType(data)} | {getCEFieldType(data)} | {getValueMapDefaultValue(data, false)} | {getValueMapDefaultValue(data, true)}{Environment.NewLine}";
            }

            content += valueMapContent;


            //Create individual Page, then the Version page 
            string localPath = String.Empty;

            localPath = wikiUpload.CombineForward(getOverviewPath(), $"{pageName}");


            //add wiki parameters
            curWikiOverview.subPageLink = localPath;
            curWikiOverview.Publisher = currentMap.detail.template.author;
            curWikiOverview.Version = versionStr;
            curWikiOverview.subPageName = $"{pageName}";
            //curWikiOverview.CEEntity = 

            await wikiUpload.createUpdatePage(content, localPath);

            //upload the Version of it
            localPath = wikiUpload.CombineForward(localPath, $"{versionStr} - {currentMap.detail.template.author}");
            await wikiUpload.createUpdatePage(content, localPath);


        }

        private async Task createOverViewPage()
        {

            string content = String.Empty;


            content += $"| FO Entity | CE Entity | Link | Direction | Version | Publisher{Environment.NewLine}";
            content += $"|--|--|--|--|--|--| {Environment.NewLine}";

            wikiOverviewList.Reverse();

            foreach (var map in wikiOverviewList)
            {
                try
                {
                    string link = wikiUpload.CombineForward(wikiUpload.wikiPath, map.subPageLink).Replace("\\", "/");

                    link = link.Replace("-", "%2D");
                    // link = HttpUtility.UrlEncode(link);
                    link = link.Replace(" ", "-");

                    content += $"|{map.FOEntity} | {map.CEEntity} | [{map.FOEntity} - {map.CEEntity}]({link}) | {DWEnums.DescriptionAttr(map.syncDirection)} | {map.Version} | {map.Publisher} {Environment.NewLine}";
                }
                catch ( Exception ex )
                {
                    
                    logger.LogError(ex.ToString());
                }
            }


            await checkCreateOverviewPage(content);

        }

        private string getValueMapDefaultValue(FieldMapping mapping, bool defaultValue)
        {
            string ret = String.Empty;


            if (mapping.valueTransforms == null ||mapping.valueTransforms.Count == 0)
                return ret;

            var transfromObj = mapping.valueTransforms.FirstOrDefault();

            if (defaultValue && transfromObj.transformType.ToUpper() == "DEFAULT")
                ret = mapping.valueTransforms.FirstOrDefault().defaultValue;
            else if (defaultValue)
                return ret;
            else
            {
                if (transfromObj.valueMap == null)
                    return ret;

                string header = $"ValueMap for {mapping.sourceField} <> {mapping.destinationField}";
                //ValueMap for 
                ret = $"[Transform](#{header.Replace(" ", "-")})";

                valueMapContent += $"## {header}{Environment.NewLine}";

                valueMapContent += $"| FO Value | CE Value {Environment.NewLine}";
                valueMapContent += $"|--|--| {Environment.NewLine}";

                foreach (var value in transfromObj.valueMap)
                {

                    valueMapContent += $"| {value.Key} | {value.Value}| {Environment.NewLine}";
                    // string test = "sdd";
                    //value.valueMap.ke
                }
                valueMapContent += Environment.NewLine;

            }
            

            return ret;
        }

        private string getCEFieldType(FieldMapping mapping)
        {
            string ret = String.Empty;

            ret = getFieldType(common.curFieldMapping.entityMappingTasks[0].legs[0].destinationEnvironment, mapping.destinationField, common.curFieldMapping.entityMappingTasks[0].legs[0].destinationSchema);

            return ret;
        }

        private string getFOFieldType(FieldMapping mapping)
        {
            string ret = String.Empty;

            ret = getFieldType(common.curFieldMapping.entityMappingTasks[0].legs[0].sourceEnvironment, mapping.sourceField, common.curFieldMapping.entityMappingTasks[0].legs[0].sourceSchema);

            return ret;
        }

        private string getFieldType(string environment, string fieldname, string objectName)
        {

            string ret = String.Empty;

            var env = common.curFieldMapping.environments.Where(x => x.name == environment).FirstOrDefault();

            //get the env
            if(env.name != null)
            {
                var entity = env.schemas.Where(x => x.name.Equals(objectName)).FirstOrDefault();

                if(entity.name != null)
                {

                    var field = entity.fields.Where(x => x.name.Equals(fieldname)).FirstOrDefault();

                    if(field.typeDetails.type != null)
                        ret = field.typeDetails.type;

                }

            }


            return ret;

        }

        private string getCEEntity()
        {
            string ret = String.Empty;

            ret = getEntityString(common.curFieldMapping.entityMappingTasks[0].legs[0].destinationEnvironment, common.curFieldMapping.entityMappingTasks[0].legs[0].destinationSchema);

            curWikiOverview.CEEntity = ret;

            return ret;
        }

        private string getFOEntity()
        {
            string ret = String.Empty;

            ret = getEntityString(common.curFieldMapping.entityMappingTasks[0].legs[0].sourceEnvironment, common.curFieldMapping.entityMappingTasks[0].legs[0].sourceSchema);

            curWikiOverview.FOEntity = ret;

            return ret;
        }


        private string getEntityString(string environment, string objectName)
        {

            string ret = String.Empty;

            var env = common.curFieldMapping.environments.Where(x => x.name == environment).FirstOrDefault();

            //get the env
            if (env.name != null)
            {
                var entity = env.schemas.Where(x => x.name.Equals(objectName)).FirstOrDefault();

                if (entity.name != null)
                {

                    ret = $"{entity.displayName} ({entity.name})";

                }

            }


            return ret;

        }

        private string getSourceFilter()
        {
            string ret = String.Empty;

            ret = common.curFieldMapping.entityMappingTasks[0].legs[0].sourceFilter == null ? "" : common.curFieldMapping.entityMappingTasks[0].legs[0].sourceFilter;

            return ret;
        }

        private string getDestinationFilter()
        {
            string ret = String.Empty;

            ret = common.curFieldMapping.entityMappingTasks[0].legs[0].reversedSourceFilter == null ? "" : common.curFieldMapping.entityMappingTasks[0].legs[0].reversedSourceFilter;

            return ret;
        }

        private DWEnums.DWSyncDirection getSyncDirection()
        {
            DWEnums.DWSyncDirection ret = DWEnums.DWSyncDirection.Both;


            //check field mapping what direction they are
            List<FieldMapping> both = common.curFieldMapping.entityMappingTasks[0].legs[0].fieldMappings.Where(x => x.syncDirection.Equals(DWEnums.DWSyncDirection.Both)).ToList();

            if (both.Count > 0)
                ret = DWEnums.DWSyncDirection.Both;
            else
            {
                List<FieldMapping> FOOnly = common.curFieldMapping.entityMappingTasks[0].legs[0].fieldMappings.Where(x => x.syncDirection.Equals(DWEnums.DWSyncDirection.FOOnly)).ToList();

                if (FOOnly.Count > 0)
                {
                    ret = DWEnums.DWSyncDirection.FOOnly;
                    //return ret;
                }

                List<FieldMapping> CEOnly = common.curFieldMapping.entityMappingTasks[0].legs[0].fieldMappings.Where(x => x.syncDirection.Equals(DWEnums.DWSyncDirection.CEOnly)).ToList();

                if (CEOnly.Count > 0)
                {
                    ret = DWEnums.DWSyncDirection.CEOnly;
                    //return ret;
                }
            }

            curWikiOverview.syncDirection = ret;

            return ret;


        }

        private string getOverviewPath()
        {
            return wikiUpload.CombineForward(path, "Current maps overview");
        }

        private async Task checkCreateOverviewPage(string content = "")
        {

            //string localPath = Path.Combine(path, "Current maps overview");

            await wikiUpload.createUpdatePage(content, getOverviewPath(), content == "" ? false : true);

        }


        private async Task checkCreateMainPage()
        {

            await wikiUpload.createUpdatePage("", path);

        }



    }
}
