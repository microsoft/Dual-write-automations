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
    public struct MapStartStopActionConflictResolution
    {
        public string option { get; set; }
        public string master { get; set; }
    }

    public class MapStartStopActionParameters
    {
        public bool skipInitialSync { get; set; }
        public MapStartStopActionConflictResolution conflictResolution { get; set; }
    }

    public struct MapStartStopActionDetail
    {
        public string tid { get; set; }
        public string pid { get; set; }
        public string cid { get; set; }
        public MapStartStopActionParameters parameters { get; set; }
    }

    public struct MapStartStopAction
    {
        public string action { get; set; }
        public List<MapStartStopActionDetail> details { get; set; }
    }

    public struct MapsRequestResponse
    {
        public string requestId { get; set; }
    }

}
