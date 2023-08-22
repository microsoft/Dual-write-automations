// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DWLibary.Struct
{

    public struct DWEnvironmentDetail
    {
        public bool isSolutionAware { get; set; }
    }

    public struct DWEnvironment
    {
        public string cid { get; set; }
        public string cname { get; set; }
        public string targetType { get; set; }
        public string displayName { get; set; }
        public DWEnvironmentDetail detail { get; set; }
        public string powerAppsEnvironment { get; set; }
        public string foEnvironment { get; set; }
    }
}
