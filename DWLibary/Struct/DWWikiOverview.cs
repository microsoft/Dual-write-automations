// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DWLibary.Struct
{
    public struct DWWikiOverview
    {
        public string FOEntity { get; set; }
        public string CEEntity { get; set; }
        public string subPageLink { get; set; }
        public string subPageName { get; set; }
        public DWEnums.DWSyncDirection syncDirection { get; set; }
        public string Version { get; set; }
        public string Publisher { get; set; }

    }
}
