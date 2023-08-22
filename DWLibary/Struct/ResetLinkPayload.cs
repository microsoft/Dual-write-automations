using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DWLibary
{
    public struct ResetLinkPayload
    {
        public string powerAppsEnvironmentName { get; set; }
        public List<ResetLinkEnvironment> environments { get; set; }
        public List<string> legalEntities { get; set; }
    }

    public struct ResetLinkEnvironment
    {
        public string targetType { get; set; }
        public string name { get; set; }
        public string displayName { get; set; }
        public string id { get; set; }
        public bool isDevInstance { get; set; }
        public string directUrl { get; set; }
    }

}
