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
    public struct DWIntegrationKeyUpdate
    {
        public Dictionary<string, List<string>> integrationKeys { get; set; }
        public string datasetName { get; set; }
    }


}
