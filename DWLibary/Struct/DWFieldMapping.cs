// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DWLibary.Struct
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public struct TypeDetails
    {
        [JsonProperty("$type")]
        public string Type { get; set; }
        public string type { get; set; }
        public int length { get; set; }
        public bool isMultiLine { get; set; }
        public int? precision { get; set; }
        public double? minimumValue { get; set; }
        public double? maximumValue { get; set; }
        public bool? isDateOnly { get; set; }
        public string relatedEntity { get; set; }
        public string navigationPropertyName { get; set; }
    }

    public struct Field
    {
        [JsonProperty("$id")]
        public string Id { get; set; }
        public string name { get; set; }
        public string displayName { get; set; }
        public bool isRequired { get; set; }
        public bool isRetrievable { get; set; }
        public bool isReadonly { get; set; }
        public TypeDetails typeDetails { get; set; }
        public string parentRelationDataSetName { get; set; }
        public string parentRelationEntitySchemaName { get; set; }
        public string nestedParentRelationEntitySchemaName { get; set; }
    }

    public struct Key
    {
        public string name { get; set; }
        public string displayName { get; set; }
        public List<string> fields { get; set; }
        public bool isIntegrationKey { get; set; }
        public bool isPrimaryKey { get; set; }
        public bool isCustomized { get; set; }
        public bool isHardcoded { get; set; }
        public string message { get; set; }
    }

    public struct AuthorizedUser
    {
        public string user { get; set; }
        public List<string> permissions { get; set; }
    }

    public struct Schema
    {
        [JsonProperty("$id")]
        public string Id { get; set; }
        public string targetType { get; set; }
        public string refreshState { get; set; }
        public string name { get; set; }
        public string displayName { get; set; }
        public List<Field> fields { get; set; }
        public List<Key> keys { get; set; }
        public string tenant { get; set; }
        public string primaryCompanyContextField { get; set; }
        public List<AuthorizedUser> authorizedUsers { get; set; }
        public string id { get; set; }
        public string owner { get; set; }
        public List<object> tags { get; set; }
        public string singletonName { get; set; }
        public List<string> companyContextFields { get; set; }
    }

    public struct FMEnvironment
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
        public bool isDevInstance { get; set; }
        public bool bypassApiHubConnector { get; set; }
        public bool needsIntegrationKey { get; set; }
        public bool excludeHardCodedIntegrationKeys { get; set; }
        public string directUrl { get; set; }
    }

    public struct ValueMap
    {
        
    }

    public struct ValueTransform
    {
        [JsonProperty("$type")]
        public string Type { get; set; }
        public string transformType { get; set; }
        //public ValueMap valueMap { get; set; }
        public Dictionary<string, string> valueMap { get; set; }
        public string defaultValue { get; set; }
        public bool createValuesOnDestination { get; set; }
    }

    public struct FieldMapping
    {
        public DWEnums.DWSyncDirection syncDirection { get; set; }
        public string sourceField { get; set; }
        public string destinationField { get; set; }
        public List<ValueTransform> valueTransforms { get; set; }
        public bool isSystemGenerated { get; set; }
        public string destinationLookupFieldRelatedEntity { get; set; }
    }

    public struct Leg
    {
        public string id { get; set; }
        public string sourceEnvironment { get; set; }
        public string sourceSchema { get; set; }
        public string sourceEnvironmentType { get; set; }
        public string sourceFilter { get; set; }
        public bool isSourceFilterEditable { get; set; }
        public string destinationEnvironment { get; set; }
        public string destinationSchema { get; set; }
        public string destinationEnvironmentType { get; set; }
        public string reversedSourceFilter { get; set; }
        public List<FieldMapping> fieldMappings { get; set; }
        public string entityFileFormat { get; set; }
        public bool deleteNonMatchingData { get; set; }
    }

    public struct EntityMappingTask
    {
        public string name { get; set; }
        public int order { get; set; }
        public string connectionSetName { get; set; }
        public string leftEnvironmentType { get; set; }
        public string centerEnvironmentType { get; set; }
        public string rightEnvironmentType { get; set; }
        public string leftEnvironment { get; set; }
        public string centerEnvironment { get; set; }
        public string leftPartitionName { get; set; }
        public string centerPartitionName { get; set; }
        public List<Leg> legs { get; set; }
    }

    public struct Version
    {
        public int major { get; set; }
        public int minor { get; set; }
        public int build { get; set; }
        public int revision { get; set; }
        public int majorRevision { get; set; }
        public int minorRevision { get; set; }
    }

    public struct TemplateIdentifier
    {
        public string name { get; set; }
        public Version version { get; set; }
        public string id { get; set; }
        public string displayName { get; set; }
    }

    public struct DWFieldMapping
    {
        public string name { get; set; }
        public string displayName { get; set; }
        public List<FMEnvironment> environments { get; set; }
        public List<string> targetTypeList { get; set; }
        public List<EntityMappingTask> entityMappingTasks { get; set; }
        public List<object> validationIssues { get; set; }
        public string projectState { get; set; }
        public bool isPQOnlineFlow { get; set; }
        public int maxCrmIo { get; set; }
        public bool useCrmOdataExport { get; set; }
        public bool createImportErrorFile { get; set; }
        public bool autoRetryFailedImportRecords { get; set; }
        public TemplateIdentifier templateIdentifier { get; set; }
        public bool isDataManagementProject { get; set; }
        public string dataManagementOperation { get; set; }
        public string tenant { get; set; }
        public bool isDualWriteProject { get; set; }
        public bool isDualWriteEnabled { get; set; }
        public bool skipInitialSync { get; set; }
        public bool areKeysMismatched { get; set; }
        public List<AuthorizedUser> authorizedUsers { get; set; }
        public string id { get; set; }
        public string owner { get; set; }
        public DateTime createdDateTime { get; set; }
        public List<object> tags { get; set; }
    }


}
