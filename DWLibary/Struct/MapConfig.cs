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
    public class MapConfig : ConfigurationElement
    {
        [ConfigurationProperty("mapName", DefaultValue = "", IsRequired = true, IsKey = true)]
        public string mapName
        {
            get { return (string)this["mapName"]; }
            set { this["mapName"] = value; }
        }

        [ConfigurationProperty("version", DefaultValue = "", IsRequired = true, IsKey = false)]
        public string version
        {
            get { return (string)this["version"]; }
            set { this["version"] = value; }
        }

        //private string _group { get; set; }
        [ConfigurationProperty("group", DefaultValue = "", IsRequired = true, IsKey = false)]
        public string group
        {
            get { return (string)this["group"]; }
            set { this["group"] = value; }
        }

        public void initSettings()
        {
            if (group != null && group != String.Empty)
            {
                // List<Group> groups = ConfigurationManager.GetSection("Groups") as List<Group>;
                foreach(Group g in GlobalVar.dwSettings.Groups)
                {
                    if (group.ToUpper() == g.name.ToUpper())
                    {
                        groupSetting = g;
                        break;
                    }
                }

                //groupSetting = GlobalVar.dwSettings.Groups.Where(x => x.name.ToUpper().Equals(group.ToUpper())).FirstOrDefault();

                if (groupSetting == null)
                    groupSetting = new Group();
            }
            else if(group == String.Empty)
            {
                groupSetting = new Group();
            }

            if (authorsStr != null && authorsStr != String.Empty)
            {
                authors = authorsStr.Split(',').ToList();
                List<string> cleanList = new List<string>();
                foreach (string author in authors)
                {
                    string localAuth = author.Trim();

                    if(localAuth.Length > 0)
                        cleanList.Add(localAuth);
                }

                authors = cleanList;
            }

            if (keysStr != null && keysStr != String.Empty)
            {
                keys = keysStr.Split(',').ToList();
                List<string> cleanList = new List<string>();
                foreach (string key in keys)
                {
                    string localKey = key.Trim();
                    localKey = localKey.Replace(" ", String.Empty);
                    if (localKey.Length > 0)
                        cleanList.Add(localKey);
                }

                keys = cleanList;
            }

        }


        [ConfigurationProperty("authors", DefaultValue = "", IsRequired = true, IsKey = false)]
        public string authorsStr
        {
            get { return (string)this["authors"]; }
            set { 
                this["authors"] = value;

               
            }
        }

        public List<string> authors { get; set; }

        [ConfigurationProperty("keys", DefaultValue = "", IsRequired = true, IsKey = false)]
        public string keysStr
        {
            get { return (string)this["keys"]; }
            set { 
                this["keys"] = value;

                
            }
        }

        public List<string> keys { get; set; }

        [ConfigurationProperty("master", DefaultValue = DWEnums.DataMaster.CE, IsRequired = true, IsKey = false)]
        public DWEnums.DataMaster master
        {
            get { return (DWEnums.DataMaster)this["master"]; }
            set { this["master"] = value; }
        }

        public Group groupSetting { get; set; }

        public MapConfig()
        {
            authors = new List<string>();
            keys = new List<string>();

            master = DWEnums.DataMaster.CE;

            
        }

    }


    //public class MapConfigs : IConfigurationSectionHandler
    //{
    //    public object Create(object parent, object configContext, XmlNode section)
    //    {
    //        List<MapConfig> obj = new List<MapConfig>();

    //        foreach (XmlNode childNode in section.ChildNodes)
    //        {
    //            if (childNode.NodeType == XmlNodeType.Comment)
    //                continue;
    //            MapConfig map = new MapConfig();
    //            foreach (XmlAttribute attrib in childNode.Attributes)
    //            {

    //                if (attrib.Name.ToUpper() == "MAPNAME")
    //                    map.mapName = attrib.Value;

    //                if (attrib.Name.ToUpper() == "VERSION")
    //                    map.version = attrib.Value;

    //                if (attrib.Name.ToUpper() == "GROUP")
    //                    map.group = attrib.Value;

    //                if (attrib.Name.ToUpper() == "MASTER")
    //                    map.master = DWEnums.GetValueFromDescription<DWEnums.DataMaster>(attrib.Value);

    //                if (attrib.Name.ToUpper() == "AUTHORS")
    //                {
    //                    if (attrib.Value != String.Empty)
    //                    {
    //                        map.authors = attrib.Value.Split(',').ToList();
    //                        List<string> cleanList = new List<string>();
    //                        foreach (string author in map.authors)
    //                        {
    //                            cleanList.Add(author.Trim());
    //                        }

    //                        map.authors = cleanList;
    //                    }
    //                }
                        

    //                if (attrib.Name.ToUpper() == "KEYS")
    //                {
    //                    if (attrib.Value != String.Empty)
    //                    {
    //                        map.keys = attrib.Value.Split(',').ToList();
    //                        List<string> cleanList = new List<string>();
    //                        foreach(string key in map.keys)
    //                        {
    //                           cleanList.Add(key.Replace(" ", String.Empty));
    //                        }

    //                        map.keys = cleanList;
    //                    }

    //                }

    //            }
    //            obj.Add(map);
    //        }
    //        return obj;
    //    }
    //}


}
