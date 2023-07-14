using DWLibary.Engines;
using DWLibary.Struct;
using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using static Azure.Core.HttpHeader;

namespace DWLibary
{
    public class DWComparison
    {
        public DWCommonEngine common01, common02;
        public DWEnvironment env01, env02;


        public string foUrl01, foUrl02;

        MapConfigs mapConfigs;
        MapConfig curMapConfig;

        List<DWMap> dwMaps01, dwMaps02;
        DWMap currentMap01, currentMap02;

        ILogger logger;

        public DWComparison(string _foUrl01, string _foUrl02, ILogger _logger)
        {

            this.foUrl01 = _foUrl01;
            this.foUrl02 = _foUrl02;
            this.logger = _logger;
        }

        private async Task init()
        {
            DWEnvCalls dWEnvCalls = new DWEnvCalls();

            GlobalVar.foEnv = foUrl01;
            EdgeUniversal uni = new EdgeUniversal(logger);
            uni.getToken();

            env01 = await dWEnvCalls.getEnvironment();


            GlobalVar.foEnv = foUrl02;
            GlobalVar.loginData = new LoginData();
            uni = new EdgeUniversal(logger);
            uni.getToken();

            env02 = await dWEnvCalls.getEnvironment();


            common01 = new DWCommonEngine(env01, logger);
            common02 = new DWCommonEngine(env02, logger);

            



        }

        public async Task runComparison()
        {

            await init();

            dwMaps01 = await common01.getDWMaps();
            dwMaps02 = await common02.getDWMaps();

            mapConfigs = GlobalVar.dwSettings.MapConfigs;

            foreach (MapConfig config in mapConfigs)
            {
                curMapConfig = config;

                if (config.mapName.Contains("header"))
                {
                    logger.LogInformation("Some");
                }

                currentMap01 = dwMaps01.Where(x => x.detail.tName.Equals(config.mapName)).FirstOrDefault();
                currentMap02 = dwMaps02.Where(x => x.detail.tName.Equals(config.mapName)).FirstOrDefault();

                if (!mapCompareExists())
                    continue;

                await common01.getFieldMappingForMaps(currentMap01);
                await common02.getFieldMappingForMaps(currentMap02);

                List<FieldMapping> mapping01, mapping02;

                

                mapping01 = common01.curFieldMapping.entityMappingTasks[0].legs[0].fieldMappings;
                mapping02 = common02.curFieldMapping.entityMappingTasks[0].legs[0].fieldMappings;


                compareFieldMapping(mapping01, mapping02, true);
                compareFieldMapping(mapping02, mapping01);

                //var difference = mapping01.Except(mapping02);

                //foreach (var item in difference)
                //{
                //    Console.WriteLine(item);
                //}


            }

            



        }


        private void compareFieldMapping(List<FieldMapping> source, List<FieldMapping> target, bool compareDetails = false)
        {

            foreach (FieldMapping map01 in source)
            {
                string prefix = $"Map {curMapConfig.mapName}, Mapping {map01.sourceField} - {map01.destinationField}:";

                FieldMapping map02 = target.Where(x => x.sourceField.Equals(map01.sourceField)).Where(y => y.destinationField.Equals(map01.destinationField)).FirstOrDefault();


                if (map02.sourceField == null)
                {
                    logger.LogWarning($"{prefix} Exists in source but not in target");

                }
                else
                {
                    if(map01.syncDirection != map02.syncDirection)
                    {
                        logger.LogWarning($"{prefix} Sync direction is different");
                    }

                    //No value transform
                    if ((map01.valueTransforms == null || !map01.valueTransforms.Any()) && (map02.valueTransforms == null || !map02.valueTransforms.Any()))
                        continue;

                    if((map01.valueTransforms == null || !map01.valueTransforms.Any()) && (map02.valueTransforms != null && map02.valueTransforms.Any()))
                    {
                        logger.LogWarning($"{prefix} Value map exixts in Target but not in Source");
                        continue;
                    }

                    if ((map01.valueTransforms != null && map01.valueTransforms.Any()) && (map02.valueTransforms == null || !map02.valueTransforms.Any()))
                    {
                        logger.LogWarning($"{prefix} Value map exixts in Target but not in Source");
                        continue;
                    }

                    //Check default value 
                    var transfromObj01 = map01.valueTransforms.FirstOrDefault();
                    var transfromObj02 = map02.valueTransforms.FirstOrDefault();

                    //Default 
                    if (transfromObj01.transformType.ToUpper() == transfromObj02.transformType.ToUpper())
                    {
                        //Default value
                        if (transfromObj01.transformType.ToUpper() == "DEFAULT")
                        {
                            if(map01.valueTransforms.FirstOrDefault().defaultValue != map02.valueTransforms.FirstOrDefault().defaultValue)
                            {
                                logger.LogWarning($"{prefix} Default value is different, Source: {map01.valueTransforms.FirstOrDefault().defaultValue}, Target: {map02.valueTransforms.FirstOrDefault().defaultValue}");
                            }
                        }
                        //valuemap
                        else
                        {
                            if(transfromObj01.valueMap == null && transfromObj02.valueMap != null)
                            {
                                logger.LogWarning($"{prefix} Value map exixts in Target but not in Source");
                            }
                            else if (transfromObj01.valueMap != null && transfromObj02.valueMap == null)
                            {
                                logger.LogWarning($"{prefix} Value map exixts in Source but not in Target");
                            }
                            //Value map in both
                            else
                            {
                                compareValueMap(transfromObj01.valueMap, transfromObj02.valueMap, prefix);
                            }
                        }

                       
                    }
                    else
                    {
                        logger.LogWarning($"{prefix} Transformation type is different, Source: {transfromObj01.transformType}, Target: {transfromObj02.transformType}");
                    }

                }


            }
        }

        private void compareValueMap(Dictionary<string,string> _map01, Dictionary<string, string> _map02, string _prefix)
        {

            var map01 = _map01.OrderBy(x => x.Key).ToList();
            var map02 = _map02.OrderBy(x => x.Key).ToList();

            var difference  = map01.Except(map02).ToList();



            foreach (var key in difference)
            {
                logger.LogWarning($"{_prefix} Value map is different in key {key}");
            }


        }

        private bool mapCompareExists()
        {
            bool ret = true;

            if (currentMap01.detail.pid == null || currentMap02.detail.pid == null)
            {
                logger.LogInformation($"Map not found {curMapConfig.mapName}");
                ret = false;
            }

            if (currentMap01.detail.pid == null && currentMap02.detail.pid != null)
            {
                logger.LogInformation($"Map found in Env02 but not Env01 {curMapConfig.mapName}");
                ret = false;
            }

            if (currentMap01.detail.pid != null && currentMap02.detail.pid == null)
            {
                logger.LogInformation($"Map found in Env01 but not Env02 {curMapConfig.mapName}");
                ret = false;
            }
            return ret;
        }

        //private void compareFielMapping(List<FieldMapping> )

    }
}
