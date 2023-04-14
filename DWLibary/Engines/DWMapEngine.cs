// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using DWLibary.Struct;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OpenQA.Selenium.DevTools.V105.Debugger;
using OpenQA.Selenium.DevTools.V105.Runtime;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace DWLibary.Engines
{
    public class DWMapEngine
    {
        MapConfigs mapConfigs;
        DWEnvironment env;
        List<DWMap> dwMaps;
        DWMap currentMap;
        
        MapConfig currentMapConfig;
        MapsRequestResponse requestResponse;
        //DWConnectionSet connectionSet;
        DWMapTemplate applyTemplate;
        DWMapTemplate previousTemplate;
        InitialSyncDetails syncDetails;
        List<DWMap> mapPreRequisites;
        List<ProjectExecutionRespons> previousResponse;
        DWCommonEngine common;
        List<ErrorMessage> localErrors { get; set; }
        string prefix;
        bool hasCurError;
       // bool requiredReload;
        ILogger logger;

        List<Thread> threads;
        List<Thread> initThreads;
        List<DWMap> pauseResumeMaps;

        public DWMapEngine(DWEnvironment _env, ILogger _logger)
        {
            env = _env;
            dwMaps = new List<DWMap>();
            currentMapConfig = new MapConfig();
            //curFieldMapping = new DWFieldMapping();

            //connectionSet = new DWConnectionSet();
            currentMap = new DWMap();
            threads = new List<Thread>();
            initThreads = new List<Thread>();
            logger = _logger;
            common = new DWCommonEngine(_env, _logger);
            //requiredReload = false;
            pauseResumeMaps = new List<DWMap>();
        }

        public DWMapEngine(DWEnvironment _env, MapConfig _mapConfig, List<DWMap> _dwMaps, DWMap _curMap, DWMapTemplate _applyTemplate, ILogger _logger, DWMapTemplate _previousTemplate)
        {
            env = _env;
            dwMaps = _dwMaps;
            currentMapConfig = _mapConfig;
           // curFieldMapping = new DWFieldMapping();
            //connectionSet = new DWConnectionSet();
            currentMap = _curMap;
            prefix = currentMap.detail.tName;
            applyTemplate = _applyTemplate;
            previousTemplate = _previousTemplate;

            logger = _logger;
            this.previousTemplate = previousTemplate;
           // requiredReload = _requiresReload;
            pauseResumeMaps = new List<DWMap>();
            common = new DWCommonEngine(_env,_logger);
        }

        public async Task applyMaps()
        {
            try
            {
                mapConfigs = GlobalVar.dwSettings.MapConfigs;
                dwMaps = await common.getDWMaps();

                await processMaps();
                logger.LogInformation("Completed applying maps");

                GlobalVar.outputErrors();
            }
            catch(Exception ex)
            {
                addError(ex.ToString());
                
            }

        }

        private void addError(string _errorMessage, string prefix = "")
        {

            if (localErrors == null)
                localErrors = new List<ErrorMessage>();

            ErrorMessage message = new ErrorMessage();
            message.error = _errorMessage;
            message.prefix = prefix;


            localErrors.Add(message);

            if (prefix != "")
                logger.LogError($"{prefix}: {_errorMessage}");
            else
                logger.LogError(_errorMessage);
        }

        public async Task generateMapConfig()
        {

            
            //todo, cleanup 
            List<DWMap> finalSortOrder = new List<DWMap>();

            dwMaps = await common.getDWMaps();


            if (GlobalVar.exportState == DWEnums.MapStatus.All)
            {
                logger.LogInformation("Initalizing maps for exporting all");
                await initAllMaps();
            }
           


            var enumerator = dwMaps.GetEnumerator();
            List<DWMap> tmpList = new List<DWMap>();
            logger.LogInformation("Getting dependencies.. this might take a while");
            try
            {
                while (enumerator.MoveNext())
                {
                    DWMap item = (DWMap)enumerator.Current;
                    item.dependency = await getTemplatePreRequisite(item.detail.template.id);

                    tmpList.Add(item);

                }
            }
            finally
            {
                enumerator.Dispose();
            }

            dwMaps = tmpList;

            logger.LogInformation("Dependencies retrived, sorting maps");
            int dependencyAmount = 0;

            while (dwMaps.Count > 0)
            {

                List<DWMap> currentDep = dwMaps.Where(x => x.dependency.Count.Equals(dependencyAmount)).ToList();

                if (dependencyAmount > 50)
                    currentDep = dwMaps.ToList();

                if (dependencyAmount == 0)
                {
                    foreach (DWMap map in currentDep)
                    {
                        finalSortOrder.Add(map);

                        dwMaps.Remove(map);
                    }
                }

                else
                {
                    foreach (DWMap map in currentDep)
                    {
                        bool okToAdd = true;

                        foreach (DWMap dep in map.dependency)
                        {
                            DWMap lookup = finalSortOrder.Where(x => x.detail.template.id.Equals(dep.detail.template.id)).FirstOrDefault();

                            DWMap tmp = dwMaps.Where(x => x.detail.template.id.Equals(dep.detail.template.id)).FirstOrDefault();

                            if (lookup.detail.template.id == null)
                            {

                                DWMap chickenEggLookup = tmp.dependency.Where(x => x.detail.template.id.Equals(map.detail.template.id)).FirstOrDefault();

                                //checks 
                                if (chickenEggLookup.detail.template.id != null && okToAdd)
                                    okToAdd = true;
                                else
                                    okToAdd = false;
                            }
                        }

                        if (okToAdd)
                        {
                            finalSortOrder.Add(map);

                            dwMaps.Remove(map);
                        }
                    }
                }

                dependencyAmount++;

                if (dependencyAmount > 200)
                {
                    logger.LogWarning("Couldn't map all maps in the correct order, remaining maps are added at the end.");
                    //break
                    foreach (DWMap map in currentDep)
                    {
                        finalSortOrder.Add(map);
                    }

                    break;
                }


            }

            await writeNewConfigFile(finalSortOrder);


        }
       
        private async Task writeNewConfigFile(List<DWMap> maps)
        {

            Configuration localConfig = GlobalVar.config;
            DWSettings dwSettings = GlobalVar.dwSettings;

            List<MapConfig> lookupList = GlobalVar.dwSettings.MapConfigs.Cast<MapConfig>().ToList();

            dwSettings.MapConfigs.Clear();
            foreach (var map in maps)
            {

                if (GlobalVar.exportState != DWEnums.MapStatus.None)
                {
                    if (map.detail.mapStatus != GlobalVar.exportState)
                    {
                        logger.LogInformation($"Export of map {map.detail.tName} skipped as its in status {map.detail.mapStatus} and only status {GlobalVar.exportState} will be exported!");
                        continue;
                    }
                }

                currentMap = map;
                MapConfig conf = new MapConfig();
                MapConfig lookup =lookupList.Where(x=>x.mapName.Equals(map.detail.tName)).FirstOrDefault();

                conf.mapName = map.detail.tName;
                conf.version = getExportVersion(lookup);
                conf.authorsStr = getAuthorString(lookup != null ? lookup.authors : null);
                conf.keysStr = await common.getCurrentKeys(currentMap);
                conf.group = lookup != null ? lookup.group : "All";
                conf.master = lookup != null ? lookup.master : DWEnums.DataMaster.CE;

                dwSettings.MapConfigs.Add(conf);
            }

            dwSettings.Solutions.Clear();

            foreach(var sol in await common.getSolutions())
            {
                dwSettings.Solutions.Add(sol);
            }

            var localDW = localConfig.GetSection("DWSettings") as DWSettings;
            localDW = dwSettings;

            string fileName = $"{GlobalVar.foEnv}_{DateTime.Now.ToString("yyyy-MM-dd")}.config";
            localConfig.SaveAs(fileName, ConfigurationSaveMode.Full);

        }

        private string getExportVersion(MapConfig _lookup = null)
        {
            string ret = String.Empty;

            if(GlobalVar.exportOption == DWEnums.ExportOptions.precise)
            {
                ret = $"{currentMap.detail.template.version.major}.{currentMap.detail.template.version.minor}.{currentMap.detail.template.version.build}.{currentMap.detail.template.version.revision}";
            }
            else
            {
                if (_lookup != null)
                    ret = _lookup.version;
                else
                    ret = "latest";
            }

            return ret;


        }



        //private async Task writeXML(List<DWMap> maps)
        //{
        //    XmlWriterSettings setting = new XmlWriterSettings();
        //    setting.ConformanceLevel = ConformanceLevel.Auto;
        //    setting.Indent = true;
        //    //setting.NewLineOnAttributes = true;
        //    setting.Encoding =  Encoding.UTF8;

        //    string fileName = $"{DateTime.Now.ToString("yyyy-MM-dd HH-mm")}_export_{GlobalVar.foEnv}.xml";
        //    logger.LogInformation($"Generating map into {fileName}");
        //    using (XmlWriter writer = XmlWriter.Create(fileName, setting))
        //    {
        //        writer.WriteStartElement("MapConfigs");
        //        foreach (var map in maps)
        //        {


        //            if(GlobalVar.exportState != DWEnums.MapStatus.None)
        //            {
        //                if(map.detail.mapStatus != GlobalVar.exportState)
        //                {
        //                    logger.LogInformation($"Export of map {map.detail.tName} skipped as its in status {map.detail.mapStatus} and only status {GlobalVar.exportState} will be exported!");
        //                    continue;
        //                }
        //            }

        //            currentMap = map;
        //            writer.WriteStartElement("Map");
        //            writer.WriteAttributeString("mapName", map.detail.tName);
        //            writer.WriteAttributeString("version", "latest");
        //            writer.WriteAttributeString("authors", getAuthorString());
        //            writer.WriteAttributeString("keys", await common.getCurrentKeys(currentMap));
        //            writer.WriteAttributeString("group", "");
        //            writer.WriteAttributeString("master", "CE");
        //            writer.WriteEndElement();
        //        }
        //        writer.WriteEndElement();
                
        //        writer.Flush();
        //    }

        //    logger.LogInformation($"Generating map successful");
        //}

        //moved to CommonEngine
        //private async Task<string> getCurrentKeys()
        //{
        //    string ret = String.Empty;

        //    if(connectionSet.id == null || connectionSet.id == String.Empty)
        //        await getConnectionSet();

        //    DWConnSetEnvironment curEnv = connectionSet.environments.Where(x => x.Key.Contains(".crm")).FirstOrDefault().Value;

        //    if (curEnv.connectionSetName != null)
        //    {
        //        Schema schema = curEnv.schemas.Where(x => x.name.Equals(currentMap.rightEntity.name)).FirstOrDefault();

        //        if (schema.id != null)
        //        {
        //            Key key = schema.keys.Where(x => x.name.Equals("userKeys")).FirstOrDefault();


        //            if(key.displayName != null)
        //                ret = string.Join(",", key.fields);
        //        }
        //    }
           

        //    return ret;
        //}

        private string getAuthorString(List<string> concatAuthors = null)
        {

            List<string> authors = new List<string>();
            string ret = String.Empty;

            if (GlobalVar.exportOption == DWEnums.ExportOptions.precise)
            {
                ret = currentMap.detail.template.author;
            }
            else
            {
                foreach (DWMapTemplate template in currentMap.detail.templates)
                {

                    string lookup = authors.FirstOrDefault(x => x.Equals(template.author));

                    if (lookup == null)
                        authors.Add(template.author);

                }

                if (concatAuthors != null && concatAuthors.Any())
                    authors = authors.Concat(concatAuthors).ToList();

                ret = String.Join(",", authors);

                if (ret == String.Empty)
                    ret = "Any";

            }


            return ret;
        }


        private async Task initMapsFromMapConfig()
        {
            foreach (MapConfig mapConfig in mapConfigs)
            {
                currentMapConfig = mapConfig;
                currentMap = dwMaps.Where(x => x.detail.tName.Equals(mapConfig.mapName)).FirstOrDefault();


                await checkAddInitMaps();

            }


            //Check and execute Threads.
            if (initThreads.Any())
            {
                executeThreads(initThreads);
            }

            dwMaps = await common.getDWMaps();

        }

        private async Task initAllMaps()
        {
            foreach (DWMap  map  in dwMaps)
            {
                currentMap = map;
                
                await checkAddInitMaps();

            }

            //Check and execute Threads.
            if (initThreads.Any())
            {
                executeThreads(initThreads);
            }
            dwMaps = await common.getDWMaps();
        }

        private async Task checkAddInitMaps()
        {

            if (currentMap.detail.pid == null)
            {
                applyTemplate = currentMap.detail.template;
                prefix = currentMap.detail.tName;
                switch (GlobalVar.executionMode)
                {
                    case DWEnums.ExecutionMode.parallel:

                        DWMapEngine localEngine = new DWMapEngine(env, currentMapConfig, dwMaps, currentMap, applyTemplate, logger, previousTemplate);

                        Thread thread = new Thread(() => localEngine.initMap().Wait());
                        //thread.Start();

                        initThreads.Add(thread);
                        logger.LogInformation($"{prefix} init has been added to run in a seperate Thread");

                        //requiredReload = true;


                        break;

                    case DWEnums.ExecutionMode.sequential:

                        await initMap();
                        break;
                }

               
            }
        }

        private async Task processMaps()
        {
            await initMapsFromMapConfig();

            foreach (MapConfig mapConfig in mapConfigs)
            {
                currentMapConfig = mapConfig;
                currentMap = dwMaps.Where(x=> x.detail.tName.Equals(mapConfig.mapName)).FirstOrDefault();

                if(currentMap.detail.tid == null)
                {
                    logger.LogWarning($"MapConfig not found in maps: {currentMapConfig.mapName} - Skip");
                    continue;
                }

                hasCurError = false;
                prefix = currentMap.detail.tName;
                //map found
                if(currentMap.detail.tid != String.Empty)
                {
                    logger.LogInformation($"Current map: {currentMap.detail.tName}");
                    applyTemplate = new DWMapTemplate();

                    if(mapConfig.version.ToUpper() == "LATEST")
                    {

                        logger.LogInformation($"{prefix}: Get latest map version");
                        applyTemplate = getLatestTemplate();

                    }
                    else
                    {
                        applyTemplate = getLatestTemplate(currentMapConfig.version);
                    }

                    if(applyTemplate.id == null)
                    {
                        logger.LogWarning($"No template found to apply, using currently applied template", prefix);
                        applyTemplate = currentMap.detail.template;
                        
                    }

                    //check init maps
                   


                    //version differs //not for start, stop, pause actions
                    if (await mapChanged())
                    {
                        previousTemplate = currentMap.detail.template;

                        switch(GlobalVar.executionMode)
                        {
                            case DWEnums.ExecutionMode.sequential:
                                await applyMapSingle();
                                break;

                            case DWEnums.ExecutionMode.parallel:
                                //If its running in parallel we need to create a new instance for local variables! 

                                DWMapEngine localEngine = new DWMapEngine(env, currentMapConfig, dwMaps, currentMap, applyTemplate, logger, previousTemplate);

                                Thread thread = new Thread(() => localEngine.applyMapSingle().Wait());
                                //thread.Start();

                                threads.Add(thread);
                                logger.LogInformation($"{prefix} has been added to run in a seperate Thread");
                                break;

                        }
                        
                            



                    }
                    else
                    {

                        //check if the map needs to be in runState:

                        logger.LogInformation($"{prefix}: Current map version would not change - skipping.");

                        logger.LogInformation($"{prefix} Target state: {currentMapConfig.groupSetting.targetStatus}, RunMode: {GlobalVar.runMode} MapStatus: {currentMap.detail.mapStatus}");
                        //checking target run status:
                        //if the map is not runnign but should be running
                        if ((currentMapConfig.groupSetting.targetStatus == DWEnums.MapStatus.Running
                            && GlobalVar.runMode == DWEnums.RunMode.deployment 
                            && (currentMap.detail.mapStatus == DWEnums.MapStatus.Stopped || currentMap.detail.mapStatus == DWEnums.MapStatus.NotRunning)) 
                            
                            || (currentMapConfig.groupSetting.targetStatus == DWEnums.MapStatus.Stopped
                            && GlobalVar.runMode == DWEnums.RunMode.deployment
                            && currentMap.detail.mapStatus == DWEnums.MapStatus.Running)
                            
                            || isTargetStateExecutionBasedOnRunMode()

                            )
                        {


                            switch (GlobalVar.executionMode)
                            {
                                case DWEnums.ExecutionMode.sequential:
                                    await executeToTargetState();
                                    break;

                                case DWEnums.ExecutionMode.parallel:
                                    //If its running in parallel we need to create a new instance for local variables! 

                                    DWMapEngine localEngine = new DWMapEngine(env, currentMapConfig, dwMaps, currentMap, applyTemplate, logger, previousTemplate);

                                    Thread thread = new Thread(() => localEngine.executeToTargetState().Wait());
                                    //thread.Start();

                                    threads.Add(thread);
                                    logger.LogInformation($"{prefix} has been added to run in a seperate Thread");
                                    break;

                            }

                        }




                    }


                }
            }



            if (GlobalVar.executionMode == DWEnums.ExecutionMode.parallel)
            {

                

                //apply integration keys
                await applyAllIntegrationKeysIfDifferent();

                executeThreads(threads);
            }


            if((GlobalVar.runMode == DWEnums.RunMode.pause 
                || GlobalVar.runMode == DWEnums.RunMode.start)
                && pauseResumeMaps.Any())
            {
                await startStopMap(GlobalVar.runMode == DWEnums.RunMode.pause ? DWEnums.StartStop.pause : DWEnums.StartStop.resume, true, true);

            }


            if(GlobalVar.runMode == DWEnums.RunMode.deployInitialSync)
            {
                await runInitialSetup();
            }

        }


        private async Task applyAllIntegrationKeysIfDifferent()
        {
            await common.getConnectionSet(true);
            foreach (MapConfig mapConfig in mapConfigs)
            {
                currentMapConfig = mapConfig;
                currentMap = dwMaps.Where(x => x.detail.tName.Equals(mapConfig.mapName)).FirstOrDefault();

                if (currentMap.detail.tid == null)
                {
                    continue;
                }

                if (await isIntegrationKeyDifferent())
                    await applyIntegrationKeys();

            }
        }


        private async Task<bool> mapChanged()
        {
            bool ret = false;

            if (GlobalVar.runMode == DWEnums.RunMode.start || GlobalVar.runMode == DWEnums.RunMode.stop || GlobalVar.runMode == DWEnums.RunMode.pause)
                return ret;
            

            //Check if the version would change
            if (ret && applyTemplate.version.major == currentMap.detail.template.version.major
                && applyTemplate.version.minor == currentMap.detail.template.version.minor
                && applyTemplate.version.build == currentMap.detail.template.version.build
                && applyTemplate.version.revision == currentMap.detail.template.version.revision
                && applyTemplate.author == currentMap.detail.template.author)
            { 
                ret = false;
                applyTemplate = currentMap.detail.template;
                logger.LogInformation($"{prefix} Map template would change, but version doesen't, duplictae map version");
            }

            if (applyTemplate.id != currentMap.detail.template.id
                        || currentMap.detail.pid == null)
            {

                // logger.LogInformation($"{prefix} Map version changes, selected map: {applyTemplate.id}, current map: {currentMap.detail.template.id}");
                ret = true;
            }

            //if (GlobalVar.runMode == DWEnums.RunMode.initialSetup)
            //    ret = true;

            //if (!ret && await isIntegrationKeyDifferent())
            //    ret = true;


            if (ret)
                logger.LogInformation($"{prefix} Map changes and will be applied!");

            return ret;
        }

        private async Task<bool> isIntegrationKeyDifferent()
        {
            bool ret = false;

            if (currentMapConfig.keys.Count == 0)
                return ret;

            var curKeys = await common.getCurrentKeyList(currentMap);

            if (curKeys.Count != currentMapConfig.keys.Count)
                return true;

            bool isEqual = Enumerable.SequenceEqual(curKeys.Select(x => x.ToUpper()).OrderBy(curKeys => curKeys), currentMapConfig.keys.Select(x=>x.ToUpper()).OrderBy(e => e));

            if (!isEqual)
                ret = true;

            if(ret)
                logger.LogInformation($"{prefix} Integrationkey difference detected!");

            return ret;
        }

        private bool isTargetStateExecutionBasedOnRunMode()
        {
            bool ret = false;
           

            switch (GlobalVar.runMode)
            {
                case DWEnums.RunMode.deployment:
                case DWEnums.RunMode.deployInitialSync:

                    return ret;
                    break;

                case DWEnums.RunMode.pause:
                    if(currentMap.detail.mapStatus == DWEnums.MapStatus.Running)
                    {
                        ret = false; //pausing maps needs to happen in one bulk 
                        pauseResumeMaps.Add(currentMap);
                    }
                    break;

                case DWEnums.RunMode.stop:
                    if (currentMap.detail.mapStatus == DWEnums.MapStatus.Running)
                    {
                        ret = true;
                    }
                    break;

                case DWEnums.RunMode.start:
                    if (currentMap.detail.mapStatus == DWEnums.MapStatus.Stopped
                        || currentMap.detail.mapStatus == DWEnums.MapStatus.NotRunning)
                        ret = true;
                    if (currentMap.detail.mapStatus == DWEnums.MapStatus.Paused)
                    {
                        pauseResumeMaps.Add(currentMap);
                        ret = false; //paused maps needs to be started collective
                    }
                        break;
            }

            return ret;
        }

        private DWEnums.StartStop getStartStopPause()
        {
            DWEnums.StartStop ret = DWEnums.StartStop.none;

            switch(GlobalVar.runMode)
            {
                case DWEnums.RunMode.deployment:
                case DWEnums.RunMode.deployInitialSync:
                    if (currentMapConfig.groupSetting.targetStatus == DWEnums.MapStatus.Running
                        && (currentMap.detail.mapStatus == DWEnums.MapStatus.Stopped || currentMap.detail.mapStatus == DWEnums.MapStatus.NotRunning))
                    {
                        ret = DWEnums.StartStop.start;
                    }


                    if (currentMapConfig.groupSetting.targetStatus == DWEnums.MapStatus.Stopped
                            && currentMap.detail.mapStatus == DWEnums.MapStatus.Running)
                    {
                        ret = DWEnums.StartStop.start;
                    }

                    break;

                case DWEnums.RunMode.start:
                    if (currentMap.detail.mapStatus == DWEnums.MapStatus.Stopped || currentMap.detail.mapStatus == DWEnums.MapStatus.NotRunning)
                        ret = DWEnums.StartStop.start;

                    if (currentMap.detail.mapStatus == DWEnums.MapStatus.Paused)
                        ret = DWEnums.StartStop.resume;

                    break;

                case DWEnums.RunMode.stop:
                    if (currentMap.detail.mapStatus == DWEnums.MapStatus.Running)
                        ret = DWEnums.StartStop.stop;
                    break;

                case DWEnums.RunMode.pause:
                    if (currentMap.detail.mapStatus == DWEnums.MapStatus.Running)
                        ret = DWEnums.StartStop.pause;

                    break;
            }

            

                return ret;
        }

        private async Task executeToTargetState()
        {

            try
            {


                await reloadCurrentMap();

                logger.LogInformation($"{prefix} Execute to target state, {currentMap.detail.mapStatus}");
                await startStopMapRetry(getStartStopPause());
                

                //if (currentMapConfig.groupSetting.targetStatus == DWEnums.MapStatus.Running
                //    && (currentMap.detail.mapStatus == DWEnums.MapStatus.Stopped || currentMap.detail.mapStatus == DWEnums.MapStatus.NotRunning))
                //{
                //    await startStopMapRetry(DWEnums.StartStop.start);
                //}

                //if (currentMapConfig.groupSetting.targetStatus == DWEnums.MapStatus.Stopped
                //    && currentMap.detail.mapStatus == DWEnums.MapStatus.Running)
                //{
                //    await startStopMapRetry(DWEnums.StartStop.stop);
                //}
            }
            catch(Exception ex)
            {
                addError(ex.ToString());
            }

        }

        private void executeThreads(List<Thread> _threads)
        {
            if (_threads.Count == 0)
                return;

            logger.LogInformation($"{_threads.Count} Total threads");

            foreach(Thread t in _threads)
            {
                logger.LogInformation($"Threadstate {t.ThreadState}");
            }

            DateTime progressOutput = DateTime.Now;
            while(true)
            {
                List<Thread> waiting = _threads.Where(x => x.ThreadState.Equals(ThreadState.Unstarted)).ToList();

                List<Thread> completed = _threads.Where(x => x.ThreadState.Equals(ThreadState.Stopped)).ToList();
                

                int running = _threads.Count - completed.Count - waiting.Count;

                

                if (running <= GlobalVar.maxThreads)
                {
                    int diff = GlobalVar.maxThreads - running;
                    
                    for(int i = 1; i <= diff; i++)
                    {
                        if (waiting.Count > 0)
                        {
                            waiting[0].Start();
                            waiting.RemoveAt(0);
                        }
                        else
                            break;
                    }

                }

                Thread.Sleep(1000);
                

                //all executed
                if (completed.Count == _threads.Count)
                    break;

                //Output progress every 30 seconds
                if(progressOutput.AddSeconds(30) <= DateTime.Now)
                {
                    logger.LogInformation($"Threads: Waiting: {waiting.Count}, Completed: {completed.Count}, Running: {running}, MaxThreads: {GlobalVar.maxThreads} ");
                    progressOutput = DateTime.Now;
                }


            }

            logger.LogInformation("All threads completed");



        }
        private async Task<bool> isInitalSyncStuck()
        {
            bool ret = false;
            await getInitalSynDetails();

            if (syncDetails.projectExecutionResponses.Count > 0)
            {
                //get first response to determine the inital sync req ID
                ProjectExecutionRespons firstResp = syncDetails.projectExecutionResponses[0];

                TaskExecutionStatus task = firstResp.taskExecutionStatuses[0];

                //if the task is running but only one instance started...
                if (task.isRunning && task.legs.Count == 1)
                {

                    if(task.legs[0].status.ToUpper() != "EXPORTING" && task.legs[0].status.ToUpper() != "IMPORTING")
                    {
                        //means the task is completed but no second task is started, checking the times. Export happens first then import happens
                        DateTime hightestTime = task.legs[0].exportFinished > task.legs[0].exportFinished ? task.legs[0].exportFinished : task.legs[0].importFinished;

                        //timeout for 5 mins
                        if (hightestTime.AddMinutes(5) < DateTime.Now)
                            ret = true;


                    }

                }


            }

            return ret;
        }

        private bool retryInitialSync()
        {
            bool ret = false;
            bool errorCountDecreased = false;
            

            if (!currentMapConfig.groupSetting.retry)
                return ret;

            //get current logs
            ProjectExecutionRespons firstResp = syncDetails.projectExecutionResponses[0];

            List<ProjectExecutionRespons> respDetails = syncDetails.projectExecutionResponses.Where(x => x.initialSyncRequestId.Equals(firstResp.initialSyncRequestId)).ToList();

            List<Error> errors = new List<Error>();

            //check if it contains lookup value contraints:
            foreach (ProjectExecutionRespons resp in respDetails)
            {
                if (resp.errorCount > 0)
                {
                    foreach (SyncLeg leg in resp.taskExecutionStatuses[0].legs)
                    {
                        if(leg.importErrors != null)
                            errors = errors.Concat(leg.importErrors.Where(x => x.errorMessage.ToUpper().Contains("The lookup value was not found".ToUpper()))).ToList();

                        if (leg.exportErrors != null)
                            errors = errors.Concat(leg.exportErrors.Where(x => x.errorMessage.ToUpper().Contains("The lookup value was not found".ToUpper()))).ToList();

                    }

                    //get previous error count
                    ProjectExecutionRespons prev = previousResponse.Where(x => x.legalEntityId.Equals(resp.legalEntityId)).FirstOrDefault();
                    if(prev.id != null)
                    {
                        if (prev.errorCount > resp.errorCount)
                            errorCountDecreased = true;
                    }

                }
            }

            

            //check the errors if the current entity was affected
            errors = errors.Where(x => x.errorMessage.ToUpper().Contains("/" + currentMap.rightEntity.name.ToUpper())).ToList();

            logger.LogDebug($"{prefix}: Errorcount inital sync failure: {errors.Count}");

            /*
             * Couldn't resolve the guid for the field: msdyn_mainrefillingwarehouse.msdyn_warehouseidentifier. The lookup value was not found: XXXX. Try this URL(s) to check if the reference data exists: https://XYZ.crm4.dynamics.com/api/data/v9.0/msdyn_warehouses?$select=msdyn_warehouseidentifier,msdyn_warehouseid&$filter=msdyn_warehouseidentifier eq 'XXXX'
             */

            if(errors.Count > 0 && previousResponse.Count > 0 && errorCountDecreased)
            {
                ret = true;
            }

            //no retry happened yet
            if (errors.Count > 0 && previousResponse.Count == 0)
                ret = true;

            previousResponse = respDetails;

            return ret;
        }

        private void logInitalSyncDetails()
        {
            if (syncDetails.projectName == null)
                return;

            if(syncDetails.projectExecutionResponses.Count > 0)
            {
                //get first response to determine the inital sync req ID
                ProjectExecutionRespons firstResp = syncDetails.projectExecutionResponses[0];

                List<ProjectExecutionRespons> respDetails = syncDetails.projectExecutionResponses.Where(x=>x.initialSyncRequestId.Equals(firstResp.initialSyncRequestId)).ToList();

                foreach (ProjectExecutionRespons resp in respDetails)
                {
                    logger.LogInformation($"{prefix}: Legal Entity: {resp.legalEntityName},  Inital sync Upserts: {resp.upsertCount}, ErrorCount: {resp.errorCount}");

                    if (resp.errorCount > 0)
                    {
                        foreach (SyncLeg leg in resp.taskExecutionStatuses[0].legs)
                        {
                            if (leg.status.ToUpper() == "ERROR" || leg.status.ToUpper() == "WARNING")
                            {
                                addError($"{leg.displayName}", prefix);
                                addError($"Export errors:", prefix);
                                generateErrorStr(leg.exportErrors);
                                addError($"Import errors:", prefix);
                                generateErrorStr(leg.importErrors);

                            }

                        }

                        
                    }
                    else
                    {
                        logger.LogInformation($"{prefix}: Inital sync successful completed");
                    }

                }

            }

        }

        private void generateErrorStr(List<Error> _errors, int count = 3)
        {

            if (_errors == null)
                return;
            int c = 0;
            foreach(Error error in _errors)
            {
                c++;

                addError(error.errorMessage);

                if (c == count)
                    break;
            }


        }

        private async Task runInitialSetup()
        {
            bool stop = false;
            //fresh status
            dwMaps = await common.getDWMaps();

            logger.LogInformation($"Starting inital sync");

            foreach (MapConfig mapConfig in mapConfigs)
            {
                try
                {
                    currentMapConfig = mapConfig;
                    currentMap = dwMaps.Where(x => x.detail.tName.Equals(mapConfig.mapName)).FirstOrDefault();

                    

                    if (currentMap.detail.tid == null)
                    {
                        logger.LogWarning($"MapConfig not found in maps: {currentMapConfig.mapName} - Skip");
                        continue;
                    }

                    applyTemplate = currentMap.detail.template;

                    previousResponse = new List<ProjectExecutionRespons>();

                    hasCurError = false;
                    prefix = currentMap.detail.tName;

                    if (currentMap.detail.mapStatus != DWEnums.MapStatus.Stopped)
                    {
                        logger.LogInformation($"{prefix}: Map is in running state and can't be ran again.");
                        continue;
                    }

                    await getDependencyForCurrentTemplate();//Replicate standard GUI behvaviour and get dependencies
                    logger.LogDebug($"{prefix}: Settings: InitalSync = {currentMapConfig.groupSetting.initialSync} TargetState={currentMapConfig.groupSetting.targetStatus}");
                    if (currentMapConfig.groupSetting.initialSync || currentMapConfig.groupSetting.targetStatus == DWEnums.MapStatus.Running)
                    {
                        //always refresh table before running inital sync to make sure all references are set properly

                        await refreshTable();
                        await startStopMap(DWEnums.StartStop.start, !currentMapConfig.groupSetting.initialSync);
                    }
                    else
                    {
                        logger.LogInformation($"{prefix}: Map is in target state keep, map was not running, skipping.");
                        continue;
                    }

                    DateTime startTime = DateTime.Now;
                    bool complete = false;

                    //check initial sync successful
                    while (!complete)
                    {

                        dwMaps = await common.getDWMaps();
                        currentMap = dwMaps.Where(x => x.detail.tName.Equals(currentMapConfig.mapName)).FirstOrDefault();

                        if (currentMap.detail.mapStatus != DWEnums.MapStatus.InitialSync)
                        {
                            //check the request. 
                            DWMapLastRequest req = currentMap.detail.lastRequests.Where(x => x.requestId.Equals(requestResponse.requestId)).FirstOrDefault();
                            await getInitalSynDetails();

                            logInitalSyncDetails();

                            //error
                            if (req.state == "3" && currentMapConfig.groupSetting.initialSync)
                            {

                                addError($"Inital sync failed, error: {req.errorMessage}", prefix);
                                if (retryInitialSync())
                                {
                                    await startStopMap(DWEnums.StartStop.start, !currentMapConfig.groupSetting.initialSync);
                                    addError($"retry inital sync", prefix);
                                    startTime = DateTime.Now;
                                }
                                else
                                {

                                    complete = true;


                                    switch (currentMapConfig.groupSetting.exceptionHandling)
                                    {
                                        case DWEnums.ExceptionHandling.ignore:
                                            //do nothing
                                            break;

                                        case DWEnums.ExceptionHandling.skip:

                                            if (currentMap.detail.mapStatus == DWEnums.MapStatus.Stopped || currentMap.detail.mapStatus == DWEnums.MapStatus.NotRunning)
                                            {
                                                logger.LogInformation($"{prefix} - Skipping inital sync, starting map.");
                                                await startStopMap(DWEnums.StartStop.start);
                                            }
                                            break;

                                        case DWEnums.ExceptionHandling.stop:
                                            stop = true;
                                            break;
                                    }
                                }


                            }
                            else
                            {
                                complete = true;
                            }


                        }

                        //wait for the next request
                        if (!complete)
                        {
                            Thread.Sleep(2000);
                            logger.LogInformation($"{prefix} Waiting for inital sync to complete...");
                        }


                        //if (startTime.AddMinutes(5) <= DateTime.Now && await isInitalSyncStuck())
                        //{
                        //    complete = true;
                        //    addError($"Inital sync is stuck", prefix);
                        //    switch (currentMapConfig.groupSetting.exceptionHandling)
                        //    {

                        //        case DWEnums.ExceptionHandling.stop:
                        //            stop = true;
                        //            break;
                        //    }

                        //}


                    }

                    if (stop)
                    {
                        addError("Execution has been aborted due to exception settings");
                        break;
                    }

                }
                catch(Exception ex)
                {
                    addError(ex.ToString());
                }


            }

            //add always all errors
            moveErrorsToGlobalVar();


        }

        private void moveErrorsToGlobalVar()
        {
            if (localErrors != null && localErrors.Count > 0)
                GlobalVar.errors = GlobalVar.errors.Concat(localErrors).ToList();
        }

        private async Task initMap()
        {
            try
            {
                if (currentMap.detail.pid == null)
                {
                    logger.LogInformation($"{prefix} Map is not initialized, initializing...");
                    await startStopMap(DWEnums.StartStop.init);


                    //if (GlobalVar.executionMode == DWEnums.ExecutionMode.sequential)
                    //    await reloadCurrentMap();

                }
            }
            catch (Exception ex)
            {
                addError(ex.ToString());
            }
        }


        private async Task applyMapSingle()
        {

            try
            {


                bool wasRunning = false;


                if (currentMap.detail.mapStatus == DWEnums.MapStatus.Running) //running
                {
                    wasRunning = true;
                    logger.LogInformation($"{prefix}: Map is running - stopping!");
                    await startStopMap(DWEnums.StartStop.stop);
                }

                //apply the version
                if (!hasCurError && applyTemplate.id != currentMap.detail.template.id)
                    await applyMapVersion();

                //apply Integration keys 
                if (!hasCurError && await isIntegrationKeyDifferent())
                    await applyIntegrationKeys();

                //Refresh tables
                if (!hasCurError)
                    await refreshTable();


                if (GlobalVar.runMode == DWEnums.RunMode.deployment)
                {
                    if (currentMapConfig.groupSetting.targetStatus == DWEnums.MapStatus.Keep
                        && wasRunning)
                        await startStopMapRetry(DWEnums.StartStop.start);

                    await executeToTargetState();

                }
            }
            catch(Exception ex)
            {
                addError(ex.ToString());
            }
          

        }



        private async Task refreshTable()
        {
            try
            {
                logger.LogInformation($"{prefix}: Refreshing tables");

                await common.getFieldMappingForMaps(currentMap, prefix);

                if(common.curFieldMapping.name != null && common.curFieldMapping.name != String.Empty)
                {
                    HttpClient client = new HttpClient();
                    DWHttp dW = new DWHttp();

                    HttpRequestMessage req = dW.buildDefaultHttpRequestPost();

                    UriBuilder uriBuilder = new UriBuilder(req.RequestUri.Scheme + "://" + req.RequestUri.Host);
                    uriBuilder.Path += $"api/Project/{common.curFieldMapping.name}/Refresh";

                    req.Content = new StringContent("{\"tokens\":[\"\"]}", Encoding.UTF8, "application/json");
                    req.RequestUri = uriBuilder.Uri;

                    //Debug Logging >>
                    logger.LogDebug($"Request URI: {req.RequestUri}");
                    //Debug Logging <<

                    var responseStr = await client.SendAsync(req);

                    if (!responseStr.IsSuccessStatusCode)
                    {
                        logger.LogInformation($"{prefix}: Refreshing table failed with code {responseStr}");
                        hasCurError = true;
                        //refresh has failed 
                    }
                    else
                    {
                        logger.LogInformation($"{prefix} Refreshing tables successful");
                    }

                }
                else
                {
                    logger.LogWarning($"{prefix} Field information not retrieved");
                }

                //get field mappings again
                await common.getFieldMappingForMaps(currentMap, prefix);

            }
            catch(Exception ex)
            {
                addError($"Refreshing tables failed", prefix);
            }
            
        }

        

        private async Task applyIntegrationKeys()
        {
            if (currentMapConfig.keys == null || currentMapConfig.keys.Count == 0)
            {
                logger.LogInformation($"{prefix}: No integration keys assigned - skipping integration keys");
                return;
            }

            await common.getConnectionSet();

            try
            {
                logger.LogInformation($"{prefix}: Applying integrationkeys {JsonConvert.SerializeObject(currentMapConfig.keys)}");

                HttpClient client = new HttpClient();
                DWHttp dW = new DWHttp();

                HttpRequestMessage req = dW.buildDefaultHttpRequestPost();

                UriBuilder uriBuilder = new UriBuilder(req.RequestUri.Scheme + "://" + req.RequestUri.Host);

                DWConnSetEnvironment conEnv = await common.getConnectionSetEnvironment(DWEnums.DataMaster.CE);

                uriBuilder.Path += $"api/dataset/{conEnv.name}/IntegrationKeys";

                req.RequestUri = uriBuilder.Uri;
                //Debug Logging >>
                logger.LogDebug($"Request URI: {req.RequestUri}");
                //Debug Logging <<

                //build the key
                DWIntegrationKeyUpdate keyUpdt = new DWIntegrationKeyUpdate();
                keyUpdt.datasetName = conEnv.name;
                keyUpdt.integrationKeys = new Dictionary<string, List<string>>();
                keyUpdt.integrationKeys.Add(currentMap.rightEntity.name, currentMapConfig.keys);

                req.Content = new StringContent(JsonConvert.SerializeObject(keyUpdt), Encoding.UTF8, "application/json");

                var responseStr = await client.SendAsync(req);

                string content = await responseStr.Content.ReadAsStringAsync();
                //Debug Logging >>
                logger.LogDebug($"Response: {responseStr} {content}");
                //Debug Logging <<

                logger.LogInformation($"{prefix} Integration keys Successful");
            }
            catch (Exception ex)
            {
                logger.LogError(ex.ToString());
            }


        }


        private async Task getDependencyForCurrentTemplate()
        {
            mapPreRequisites = await getTemplatePreRequisite(applyTemplate.id);
        }

        private async Task<List<DWMap>> getTemplatePreRequisite(string mapId)
        {
            List<DWMap> ret = new List<DWMap>();
            try
            {
                

                HttpClient client = new HttpClient();
                DWHttp dW = new DWHttp();

                HttpRequestMessage req = dW.buildDefaultHttpRequestPost();

                UriBuilder uriBuilder = new UriBuilder(req.RequestUri);
                uriBuilder.Path += $"GetTemplatePrerequisiteSequences/{env.cid}";
                req.RequestUri = uriBuilder.Uri;
                //Debug Logging >>
                logger.LogDebug($"Request URI: {req.RequestUri}");
                //Debug Logging <<
                req.Content = new StringContent($"[\"{mapId}\"]", Encoding.UTF8, "application/json");

                var responseStr = await client.SendAsync(req);

                string content = await responseStr.Content.ReadAsStringAsync();

                //Debug Logging >>
                logger.LogDebug($"Response: {responseStr} {content}");
                //Debug Logging <<

                List<List<string>> dependecy = JsonConvert.DeserializeObject<List<List<string>>>(content);

                mapPreRequisites = new List<DWMap>();
                foreach (string id in dependecy[0])
                {
                    if (id == mapId)
                        continue;
                    DWMap map = dwMaps.Where(x=> x.detail.tid.Equals(id)).FirstOrDefault();

                    if(map.detail.tid !=null)
                        ret.Add(map);
                }


            }
            catch(Exception ex)
            {

            }

            return ret;
            
        }

        private async Task getInitalSynDetails()
        {


            logger.LogInformation($"{prefix}: Get Inital sync details");
            try
            {
                HttpClient client = new HttpClient();
                DWHttp dW = new DWHttp();

                HttpRequestMessage req = dW.buildDefaultHttpRequestGet();

                UriBuilder uriBuilder = new UriBuilder(req.RequestUri.Scheme + "://" + req.RequestUri.Host);
                uriBuilder.Path += $"api/ProjectExecutionWithCompanies/{currentMap.detail.pid}";
                req.RequestUri = uriBuilder.Uri;


                //Debug Logging >>
                logger.LogDebug($"Request URI: {req.RequestUri}");
                //Debug Logging <<

                var responseStr = await client.SendAsync(req);

                string content = await responseStr.Content.ReadAsStringAsync();

                //Debug Logging >>
                logger.LogDebug($"Response: {responseStr} {content}");
                //Debug Logging <<

                syncDetails = JsonConvert.DeserializeObject<InitialSyncDetails>(content);



            }
            catch (Exception ex)
            {
                addError(ex.ToString());
            }
        }

        

        private async Task startStopMapRetry(DWEnums.StartStop _startStop, bool _skipInitalSync = true)
        {
            int retryCount = 1;

            if(_startStop == DWEnums.StartStop.none)
            {
                addError($"{prefix} - Could not determine start action");
                moveErrorsToGlobalVar();
                return;
            }

            for(int i = 0; i <= retryCount; i++)
            {

                await startStopMap(_startStop, _skipInitalSync);

                if (hasCurError && i < retryCount && _startStop == DWEnums.StartStop.start)
                {

                    logger.LogInformation($"{prefix} has errored, retry applying integration keys & refresh tables");
                    hasCurError = false;
                    await applyIntegrationKeys();
                    await refreshTable();
                }
                else
                {
                    break;
                }

               


            }

            //apply previous template if failed

            if(hasCurError && previousTemplate.id != null && _startStop == DWEnums.StartStop.start)
            {

                //Add error to the global log
                moveErrorsToGlobalVar();

                applyTemplate = previousTemplate;

                await applyMapVersion();
                
                await refreshTable();

                await startStopMap(_startStop, _skipInitalSync);

                addError($"Previous map template has been applied due to errors ", prefix);

            }
            else if (hasCurError && previousTemplate.id == null && _startStop == DWEnums.StartStop.start)
            {
                moveErrorsToGlobalVar();
            }
            


        }

        private async Task startStopMap(DWEnums.StartStop _startStop, bool _skipInitalSync = true, bool _resumePause = false)
        {
            HttpClient client = new HttpClient();
            client.Timeout = new TimeSpan(0,0,180);
            DWHttp dW = new DWHttp();

            HttpRequestMessage req = dW.buildDefaultHttpRequestPost();
           

            UriBuilder uriBuilder = new UriBuilder(req.RequestUri);
            uriBuilder.Path += $"Start";
            DWEnums.MapStatus targetStatus = DWEnums.MapStatus.None;

            MapStartStopAction action = new MapStartStopAction();
            
            switch(_startStop)
            {
                case DWEnums.StartStop.stop:
                    action.action = "4";
                    targetStatus = DWEnums.MapStatus.Stopped;
                    break;

                case DWEnums.StartStop.pause:
                    action.action = "5";
                    targetStatus = DWEnums.MapStatus.Paused;
                    break;

                case DWEnums.StartStop.start:
                    action.action = "1";
                    targetStatus = DWEnums.MapStatus.Running;
                    break;

                case DWEnums.StartStop.resume:
                    action.action = "6";
                    targetStatus = DWEnums.MapStatus.Running;
                    break;

                case DWEnums.StartStop.init:
                    action.action = "8";
                    break;
            }

            if (_resumePause)
            {
                action.details = new List<MapStartStopActionDetail>();
                foreach (var map in pauseResumeMaps)
                {
                    

                    MapStartStopActionDetail actionDetail = new MapStartStopActionDetail();
                    actionDetail.tid = map.detail.template.id;

                    //if no PID 
                    if (_startStop != DWEnums.StartStop.init)
                        actionDetail.pid = map.detail.pid;

                    actionDetail.cid = env.cid;


                    MapStartStopActionParameters actionParam = new MapStartStopActionParameters();
                   // actionDetail.parameters = actionParam;

                    action.details.Add(actionDetail);
                }

            }
            else
            {

                action.details = new List<MapStartStopActionDetail>();

                MapStartStopActionDetail actionDetail = new MapStartStopActionDetail();
                actionDetail.tid = applyTemplate.id;

                //if no PID 
                if (_startStop != DWEnums.StartStop.init)
                    actionDetail.pid = currentMap.detail.pid;

                actionDetail.cid = env.cid;


                MapStartStopActionParameters actionParam = new MapStartStopActionParameters();
                actionParam.skipInitialSync = _skipInitalSync;

                MapStartStopActionConflictResolution actionParamConflict = new MapStartStopActionConflictResolution();
                actionParamConflict.master = DWEnums.DescriptionAttr(currentMapConfig != null ? currentMapConfig.master : DWEnums.DataMaster.CE);
                actionParamConflict.option = "1"; //can set by parameter

                actionParam.conflictResolution = actionParamConflict;
                actionDetail.parameters = actionParam;

                if (_startStop == DWEnums.StartStop.init)
                    actionDetail.parameters = null;

                action.details.Add(actionDetail);
            }

            req.RequestUri = uriBuilder.Uri;

            //Debug Logging >>
            logger.LogDebug($"Request URI: {req.RequestUri}");
            //Debug Logging <<

            string payload = JsonConvert.SerializeObject(action, Newtonsoft.Json.Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            logger.LogDebug($"{prefix} startStopInitPayload: {payload}");

            req.Content = new StringContent(payload, Encoding.UTF8, "application/json");

            var responseStr = await client.SendAsync(req);

            //responseStr.StatusCode 
            string content = await responseStr.Content.ReadAsStringAsync();

            //Debug Logging >>
            logger.LogDebug($"Response: {responseStr} {content}");
            //Debug Logging <<

            requestResponse = JsonConvert.DeserializeObject<MapsRequestResponse>(content);

            while(!await checkRequestResponseStatus(targetStatus, _resumePause))
            {
                //waiting to be stopped/strarted
                Thread.Sleep(1000);
                if (_resumePause)
                    logger.LogInformation("Waiting for all maps to resume / pause");
                else
                    logger.LogInformation($"{prefix}: Waiting for action {_startStop} to be completed");
            }

        }

        private async Task<bool> checkRequestResponseStatus(DWEnums.MapStatus _targetStatus, bool _resumePause = false)
        {
            bool ret = false;

            try
            {
                HttpClient client = new HttpClient();
                DWHttp dW = new DWHttp();

                HttpRequestMessage req = dW.buildDefaultHttpRequestGet();

                UriBuilder uriBuilder = new UriBuilder(req.RequestUri);
                uriBuilder.Path += $"Status/{requestResponse.requestId}";
                req.RequestUri = uriBuilder.Uri;

                //Debug Logging >>
                logger.LogDebug($"Request URI: {req.RequestUri}");
                //Debug Logging <<

                var responseStr = await client.SendAsync(req);

                string content = await responseStr.Content.ReadAsStringAsync();

                //Debug Logging >>
                logger.LogDebug($"Response: {responseStr} {content}");
                //Debug Logging <<

                List<DWMap> result = JsonConvert.DeserializeObject<List<DWMap>>(content);


                if (_resumePause)
                {
                    ret = true;
                    //need to loop through each one of them and check if completed
                    foreach(var detail in result)
                    {
                        var pauseResult = detail.detail.lastRequests.Where(x => x.requestId.Equals(requestResponse.requestId)).FirstOrDefault();

                        //means still processing
                        if (pauseResult.state != "2" && pauseResult.state != "3")
                        {
                            ret = false;
                            break;
                        }



                    }
                }
                else
                {
                    DWMapLastRequest reqResp = result[0].detail.lastRequests.Where(x => x.requestId.Equals(requestResponse.requestId)).FirstOrDefault();

                    if (reqResp.state == "2")
                        ret = true;

                    if (reqResp.state == "3")
                    {
                        //error state
                        ret = true;
                        hasCurError = true;
                        addError($"Error: {reqResp.errorMessage}", prefix);
                    }
                }

            }
            catch (Exception ex)
            {
                hasCurError = true;
                addError($"Error: Unknown error", prefix);
                ret = true;
            }

            return ret;
        }

        private async Task reloadCurrentMap()
        {

            //needed to reload the current applied template
            List<DWMap> localMaps = await common.getDWMaps();

            currentMap = localMaps.Where(x => x.detail.tName.Equals(currentMapConfig.mapName)).FirstOrDefault();
        }


        private async Task applyMapVersion()
        {

            try
            {


                logger.LogInformation($"{prefix}: Applying map version {applyTemplate.id} - {applyTemplate.version.major}.{applyTemplate.version.minor}.{applyTemplate.version.build}.{applyTemplate.version.revision}");
                HttpClient client = new HttpClient();
                DWHttp dW = new DWHttp();

                HttpRequestMessage req = dW.buildDefaultHttpRequestPost();

                UriBuilder uriBuilder = new UriBuilder(req.RequestUri);
                uriBuilder.Path += $"SolutionAware/{env.cid}/SwitchActive/{applyTemplate.id}";
                uriBuilder.Query = $"pid={currentMap.detail.pid}";

                req.RequestUri = uriBuilder.Uri;

                //Debug Logging >>
                logger.LogDebug($"Request URI: {req.RequestUri}");
                //Debug Logging <<

                req.Content = new StringContent(applyTemplate.id, Encoding.UTF8, "application/json");

                var responseStr = await client.SendAsync(req);

                //responseStr.StatusCode 
                string content = await responseStr.Content.ReadAsStringAsync();

                //Debug Logging >>
                logger.LogDebug($"Response: {responseStr} {content}");
                //Debug Logging <<

                await reloadCurrentMap();
            }
            catch (Exception ex)
            {
                addError(ex.ToString(), prefix);
                hasCurError = true;
            }

        }


        private DWMapTemplate getLatestTemplate(string _version)
        {
            DWMapTemplate template = new DWMapTemplate();

            System.Version version = null;
            try
            {
                version = System.Version.Parse(_version);
                template = getLatestTemplate(version.Major, version.Minor, version.Build, version.Revision);
            }
            catch
            {
                addError($"Version could not be parsed, applying latest", prefix);
                template = getLatestTemplate();
            }

            return template;

        }

        private DWMapTemplate getLatestTemplate(int major = 0, int minor = 0, int build = 0, int revision = 0)
        {
            DWMapTemplate ret = new DWMapTemplate() ;
            List<DWMapTemplate> localTmps = new List<DWMapTemplate>();

            if (currentMapConfig.authors.Count > 0)
            {
                foreach(string author in currentMapConfig.authors)
                {
                    if (author.ToUpper() == "ANY")
                    {
                        localTmps = currentMap.detail.templates;
                        break;
                    }
                        

                    localTmps = localTmps.Concat(currentMap.detail.templates.Where(x => x.author.Equals(author))).ToList();
                }
                
            }

            //additional checks / removal of NULL Template records
            List<DWMapTemplate> toRemove = localTmps.Where(x => x.id.Equals(null)).ToList();

            foreach (DWMapTemplate tmp in toRemove)
            {
                localTmps.Remove(tmp);
            }

            if (major != 0)
                localTmps = localTmps.Where(x => x.version.major.Equals(major)).Where(x => x.version.minor.Equals(minor)).Where(x => x.version.Equals(build)).Where(x => x.version.revision.Equals(revision)).ToList();

            //order the maps by version

            localTmps = localTmps.OrderByDescending(x => x.version.major).ThenByDescending(x => x.version.minor).ThenByDescending(x => x.version.build).ThenByDescending(x => x.version.revision)
                .ThenBy(x => x.author)
                .ThenBy(x => x.id)
                .ToList();



            //latest
            if(localTmps.Count > 0)
                ret = localTmps[0];

            if(currentMap.detail.template.version.major == ret.version.major
                && currentMap.detail.template.version.minor == ret.version.minor
                && currentMap.detail.template.version.build == ret.version.build  
                && currentMap.detail.template.version.revision == ret.version.revision
                && currentMap.detail.template.author == ret.author)
            {

                ret = currentMap.detail.template;
            }


            return ret;
        }


        //public async Task<List<DWMap>> getDWMaps()
        //{

        //    List<DWMap> ret = new List<DWMap>();
        //    try
        //    {
        //        HttpClient client = new HttpClient();
        //        DWHttp dW = new DWHttp();

        //        HttpRequestMessage req = dW.buildDefaultHttpRequestGet();

        //        UriBuilder uriBuilder = new UriBuilder(req.RequestUri);
        //        uriBuilder.Path += $"Entities";
        //        uriBuilder.Query = $"targetType=AX&cid={env.cid}";

        //        req.RequestUri = uriBuilder.Uri;


        //        var responseStr = await client.SendAsync(req);

        //        string content = await responseStr.Content.ReadAsStringAsync();

        //        ret = JsonConvert.DeserializeObject<List<DWMap>>(content);



        //    }
        //    catch (Exception ex)
        //    {

        //    }

        //    return ret;
        //}

    }
}
