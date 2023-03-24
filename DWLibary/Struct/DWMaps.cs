// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using OpenQA.Selenium.IE;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DWLibary.Struct
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public struct DWMapLeftEntity
    {
        public string targetType { get; set; }
        public string name { get; set; }
        public string displayName { get; set; }
    }

    public struct DWMapRightEntity
    {
        public string targetType { get; set; }
        public string name { get; set; }
        public string displayName { get; set; }
    }

    public struct DWMapVersion
    {
        public int major { get; set; }
        public int minor { get; set; }
        public int build { get; set; }
        public int revision { get; set; }
        public int majorRevision { get; set; }
        public int minorRevision { get; set; }
    }

    public struct DWMapTemplate
    {
        public string id { get; set; }
        public string name { get; set; }
        public bool readOnly { get; set; }
        public string displayName { get; set; }
        public string author { get; set; }
        public DWMapVersion version { get; set; }
        public List<string> tags { get; set; }
        public string description { get; set; }
        public DateTime createdDateTime { get; set; }
    }


    public struct DWMapLastRequest
    {
        public string requestId { get; set; }
        public string action { get; set; }
        public DateTime createdOn { get; set; }
        public DateTime modifiedOn { get; set; }
        public string state { get; set; }
        public string errorMessage { get; set; }
        public string errorUri { get; set; }
        public string rootActivityId { get; set; }
    }


    public struct DWMapDetail
    {
        public string tid { get; set; }
        public string tName { get; set; }
        public List<string> tags { get; set; }
        public List<DWMapTemplate> templates { get; set; }
        public DWMapTemplate template { get; set; }
        public string pid { get; set; }
        public DWMapLastRequest lastRequest { get; set; }
        private string _state { get; set; }
        public string state
        {
            get
            {
                return _state;
            }
            set
            {
                _state = value;
                mapStatus = (DWEnums.MapStatus)Convert.ToInt16(value);
            }
        }

        public DWEnums.MapStatus mapStatus { get; set; }
        public List<string> actions { get; set; }
        public List<DWMapLastRequest> lastRequests { get; set; }
        public bool isProjectDeleted { get; set; }

        public Group group { get; set; }
    }

    public struct DWMap
    {
        public DWMapLeftEntity leftEntity { get; set; }
        public DWMapRightEntity rightEntity { get; set; }
        public DWMapDetail detail { get; set; }

        public List<DWMap> dependency { get; set; } 
    }


}
