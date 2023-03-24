// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace DWLibary
{
    public class Group : ConfigurationElement
    {
        [ConfigurationProperty("name", DefaultValue = "", IsRequired = true, IsKey = true)]
        public string name
        {
            get { return (string)this["name"]; }
            set { this["name"] = value; }
        }

        [ConfigurationProperty("initialSync", DefaultValue = false, IsRequired = true, IsKey = false)]
        public bool initialSync
        {
            get { return (bool)this["initialSync"]; }
            set { this["initialSync"] = value; }
        }

        [ConfigurationProperty("retry", DefaultValue = false, IsRequired = true, IsKey = false)]
        public bool retry 
        {
            get { return (bool) this["retry"]; }
            set { this["retry"] = value; }
        }

        [ConfigurationProperty("targetState", DefaultValue = DWEnums.MapStatus.Keep, IsRequired = true, IsKey = false)]
        public DWEnums.MapStatus targetStatus
        {
            get { return (DWEnums.MapStatus)this["targetState"]; }
            set { this["targetState"] = value; }
        }

        [ConfigurationProperty("exceptionHandling", DefaultValue = DWEnums.ExceptionHandling.ignore, IsRequired = true, IsKey = false)]
        public DWEnums.ExceptionHandling exceptionHandling
        {
            get { return (DWEnums.ExceptionHandling)this["exceptionHandling"]; }
            set { this["exceptionHandling"] = value; }
        }

        public Group()
        {
            name = String.Empty;
            initialSync = false;
            retry = false;
            targetStatus = DWEnums.MapStatus.Running;
            exceptionHandling = DWEnums.ExceptionHandling.ignore;
        }

    }

   


    //public class Groups : IConfigurationSectionHandler
    //{
    //    public object Create(object parent, object configContext, XmlNode section)
    //    {
    //        List<Group> obj = new List<Group>();

    //        foreach (XmlNode childNode in section.ChildNodes)
    //        {

    //            Group grp = new Group();

    //            if (childNode.NodeType == XmlNodeType.Comment)
    //                continue;

    //            foreach (XmlAttribute attrib in childNode.Attributes)
    //            {

    //                if (attrib.Name.ToUpper() == "NAME")
    //                    grp.name = attrib.Value;

    //                if (attrib.Name.ToUpper() == "INITIALSYNC")
    //                    grp.initialSync = Convert.ToBoolean(attrib.Value);

    //                if (attrib.Name.ToUpper() == "RETRY")
    //                    grp.retry = Convert.ToBoolean(attrib.Value);

    //                if (attrib.Name.ToUpper() == "TARGETSTATE")
    //                    grp.targetStatus = DWEnums.GetValueFromDescription<DWEnums.MapStatus>(attrib.Value);

    //                if (attrib.Name.ToUpper() == "EXCEPTIONHANDLING")
    //                    grp.exceptionHandling = DWEnums.GetValueFromDescription<DWEnums.ExceptionHandling>(attrib.Value);



    //            }
    //            obj.Add(grp);
    //        }
    //        return obj;
    //    }
    //}
}
