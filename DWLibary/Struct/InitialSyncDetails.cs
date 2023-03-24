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
    public struct ExportRangeInfo
    {
        public string start { get; set; }
        public string end { get; set; }
    }

    public struct Error
    {
        public string recordId { get; set; }
        public string sourceField { get; set; }
        public string errorMessage { get; set; }
    }

   

    public struct TaskExecutionStatus
    {
        public string name { get; set; }
        public string status { get; set; }
        public bool isRunning { get; set; }
        public List<SyncLeg> legs { get; set; }
    }

    public struct ProjectExecutionRespons
    {
        public string responseId { get; set; }
        public string displayName { get; set; }
        public string projectName { get; set; }
        public string legalEntityName { get; set; }
        public string legalEntityId { get; set; }
        public string schedule { get; set; }
        public DateTime submittedOn { get; set; }
        public string executionStatus { get; set; }
        public DateTime lastExecutionStatusChangeOn { get; set; }
        public List<TaskExecutionStatus> taskExecutionStatuses { get; set; }
        public string runtimeMappingExecutionRequestName { get; set; }
        public bool isDataManagementProject { get; set; }
        public string dataManagementOperation { get; set; }
        public string tenant { get; set; }
        public string initialSyncRequestId { get; set; }
        public string id { get; set; }
        public int upsertCount { get; set; }
        public int errorCount { get; set; }
        public string owner { get; set; }
        public DateTime createdDateTime { get; set; }
        public List<object> tags { get; set; }
    }

    public class SyncLeg
    {
        public string displayName { get; set; }
        public string status { get; set; }
        public string details { get; set; }
        public string exportStatus { get; set; }
        public string exportDetails { get; set; }
        public string exportJob { get; set; }
        public DateTime exportStarted { get; set; }
        public DateTime exportFinished { get; set; }
        public int exportRecordCount { get; set; }
        public int exportRecordErrorCount { get; set; }
        public string exportNewVersionToken { get; set; }
        public ExportRangeInfo exportRangeInfo { get; set; }
        public string importStatus { get; set; }
        public string importDetails { get; set; }
        public string importJob { get; set; }
        public DateTime importStarted { get; set; }
        public DateTime importFinished { get; set; }
        public int importRecordsInsertedCount { get; set; }
        public int importRecordsUpdatedCount { get; set; }
        public int importRecordsErrorCount { get; set; }
        public List<Error> exportErrors { get; set; }
        public string exportUri { get; set; }
        public List<Error> importErrors { get; set; }
        public string importErrorUri { get; set; }
    }
    public struct InitialSyncDetails
    {
        public string projectName { get; set; }
        public List<ProjectExecutionRespons> projectExecutionResponses { get; set; }
    }


}
