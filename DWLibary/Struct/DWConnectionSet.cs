// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DWLibary.Struct
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);


    public struct DWConnSetEnvironment
    {
        public string name { get; set; }
        public string connectionSetName { get; set; }
        public string targetType { get; set; }
        public List<object> sharedEnums { get; set; }
        public List<Schema> schemas { get; set; }
        public string connectionDisplayName { get; set; }
        public string environmentDisplayName { get; set; }
        public string metadataUrl { get; set; }
        public string environmentInfo { get; set; }
        public string powerAppsEnvironment { get; set; }
        public string directUrl { get; set; }
        public bool isDevInstance { get; set; }
        public bool bypassApiHubConnector { get; set; }
        public bool needsIntegrationKey { get; set; }
        public bool excludeHardCodedIntegrationKeys { get; set; }
    }



    public struct DataPartitionMapping
    {
        public string firstPartitionEnvName { get; set; }
        public string firstPartition { get; set; }
        public string secondPartitionEnvName { get; set; }
        public string secondPartition { get; set; }
    }

    public struct Left
    {
        public string name { get; set; }
        public string id { get; set; }
    }

    public struct Right
    {
        public string name { get; set; }
        public string id { get; set; }
    }

    public struct Mapping
    {
        public Left left { get; set; }
        public Right right { get; set; }
    }

    public struct LegalEntityMappings
    {
        public string leftEnvironment { get; set; }
        public string rightEnvironment { get; set; }
        public List<Mapping> mappings { get; set; }
    }

    public struct ConflictResolution
    {
        public string option { get; set; }
        public string master { get; set; }
    }

    public struct Threshold
    {
        public int count { get; set; }
        public int interval { get; set; }
        public string unitOfTime { get; set; }
    }

    public struct AlertSetting
    {
        public string name { get; set; }
        public string state { get; set; }
        public List<string> errorTypes { get; set; }
        public Threshold threshold { get; set; }
        public string action { get; set; }
    }

    public struct Result
    {
        public string message { get; set; }
        public string status { get; set; }
    }

    public struct SolutionAwareRequest
    {
        public string requestId { get; set; }
        public string action { get; set; }
        public DateTime createdOn { get; set; }
        public DateTime modifiedOn { get; set; }
        public string state { get; set; }
        public Result result { get; set; }
        public string rootActivityId { get; set; }
    }

    public struct ScheduledRequest
    {
        public string activityType { get; set; }
        public DateTime lastExecutionTime { get; set; }
        public string frequency { get; set; }
        public int interval { get; set; }
    }

    public struct DualWriteDetail
    {
        public DateTime trialExpiresOn { get; set; }
        public LegalEntityMappings legalEntityMappings { get; set; }
        public ConflictResolution conflictResolution { get; set; }
        public List<AlertSetting> alertSettings { get; set; }
        public List<object> autoPauseThresholdSettings { get; set; }
        public bool isOnFirstPartyAuth { get; set; }
        public bool isSolutionAware { get; set; }
        public List<SolutionAwareRequest> solutionAwareRequests { get; set; }
        public List<object> actionDetails { get; set; }
        public List<string> errorTableNames { get; set; }
        public string dualWriteScheduleLogicAppRunId { get; set; }
        public List<ScheduledRequest> scheduledRequests { get; set; }
        public bool isTrialEnvironment { get; set; }
        public bool errorTableUpdateInProgress { get; set; }
        public string catchupCleanupTriggerName { get; set; }
    }

    public struct DWConnectionSet
    {
        public string name { get; set; }
        public string displayName { get; set; }
        public Dictionary<string, DWConnSetEnvironment> environments { get; set; }
        public List<string> targetTypeList { get; set; }
        public List<DataPartitionMapping> dataPartitionMappings { get; set; }
        public string tenant { get; set; }
        public bool forDualWrite { get; set; }
        public DualWriteDetail dualWriteDetail { get; set; }
        public bool bypassApiHubConnector { get; set; }
        public string id { get; set; }
        public string owner { get; set; }
        public DateTime createdDateTime { get; set; }
        public List<object> tags { get; set; }
    }


}
