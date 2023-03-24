// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DWLibary.Struct
{

    public class DWSolution
    {
        public string description { get; set; }
        public string friendlyname { get; set; }
        public bool ismanaged { get; set; }
        public string solutionid { get; set; }
        public string id { get; set; }
        public string uniquename { get; set; }
        public string version { get; set; }
    }

}
