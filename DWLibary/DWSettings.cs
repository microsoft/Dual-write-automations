// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace DWLibary
{

    public class ADOWikiParameter : ConfigurationSection
    {

        public ADOWikiParameter() { }

        public ADOWikiParameter(string key)
        {
            Key = key;
            //ReportType = reportType;
        }

        [ConfigurationProperty("key", DefaultValue = "", IsRequired = true, IsKey = true)]
        public string Key
        {
            get { return (string)this["key"]; }
            set { this["key"] = value; }
        }

        [ConfigurationProperty("value", DefaultValue = "", IsRequired = true, IsKey = true)]
        public string Value
        {
            get { return (string)this["value"]; }
            set { this["value"] = value; }
        }
    }


    public class DWSettings : ConfigurationSection
    {

        [ConfigurationProperty("ADOWikiParameters", IsDefaultCollection = false)]
        [ConfigurationCollection(typeof(ADOWikiParameters),
         AddItemName = "add",
        ClearItemsName = "clear",
        RemoveItemName = "remove")]
        public ADOWikiParameters ADOWikiParameters
        {
            get
            {
                return (ADOWikiParameters)base["ADOWikiParameters"];
            }
        }


        [ConfigurationProperty("Solutions", IsDefaultCollection = false)]
        [ConfigurationCollection(typeof(Solutions),
         AddItemName = "Solution",
        ClearItemsName = "clear",
        RemoveItemName = "remove")]
        public Solutions Solutions
        {
            get
            {
                return (Solutions)base["Solutions"];
            }
        }

        [ConfigurationProperty("Groups", IsDefaultCollection = false)]
        [ConfigurationCollection(typeof(Groups),
         AddItemName = "Group",
        ClearItemsName = "clear",
        RemoveItemName = "remove")]
        public Groups Groups
        {
            get
            {
                return (Groups)base["Groups"];
            }
        }

        [ConfigurationProperty("MapConfigs", IsDefaultCollection = false)]
        [ConfigurationCollection(typeof(MapConfigs),
         AddItemName = "Map",
        ClearItemsName = "clear",
        RemoveItemName = "remove")]
        public MapConfigs MapConfigs
        {
            get
            {
                return (MapConfigs)base["MapConfigs"];
            }
        }
    }

    public class ADOWikiParameters : ConfigurationElementCollection
    {
        public ADOWikiParameters()
        {
            // Console.WriteLine("ServiceCollection Constructor");
        }

        public ADOWikiParameter this[int index]
        {
            get { return (ADOWikiParameter)BaseGet(index); }
            set
            {
                if (BaseGet(index) != null)
                {
                    BaseRemoveAt(index);
                }
                BaseAdd(index, value);
            }
        }

        public void Add(ADOWikiParameter serviceConfig)
        {
            BaseAdd(serviceConfig);
        }

        public void Clear()
        {
            BaseClear();
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new ADOWikiParameter();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((ADOWikiParameter)element).Key;
        }

        public void Remove(ADOWikiParameter serviceConfig)
        {
            BaseRemove(serviceConfig.Key);
        }

        public void RemoveAt(int index)
        {
            BaseRemoveAt(index);
        }

        public void Remove(string name)
        {
            BaseRemove(name);
        }
    }

    public class Solutions : ConfigurationElementCollection
    {
        public Solutions()
        {
           // Console.WriteLine("ServiceCollection Constructor");
        }

        public Solution this[int index]
        {
            get { return (Solution)BaseGet(index); }
            set
            {
                if (BaseGet(index) != null)
                {
                    BaseRemoveAt(index);
                }
                BaseAdd(index, value);
            }
        }

        public void Add(Solution serviceConfig)
        {
            BaseAdd(serviceConfig);
        }

        public void Clear()
        {
            BaseClear();
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new Solution();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((Solution)element).Name;
        }

        public void Remove(Solution serviceConfig)
        {
            BaseRemove(serviceConfig.Name);
        }

        public void RemoveAt(int index)
        {
            BaseRemoveAt(index);
        }

        public void Remove(string name)
        {
            BaseRemove(name);
        }
    }

    public class Groups : ConfigurationElementCollection
    {
        public Groups()
        {
            //Console.WriteLine("ServiceCollection Constructor");
        }

        public Group this[int index]
        {
            get { return (Group)BaseGet(index); }
            set
            {
                if (BaseGet(index) != null)
                {
                    BaseRemoveAt(index);
                }
                BaseAdd(index, value);
            }
        }

        public void Add(Group serviceConfig)
        {
            BaseAdd(serviceConfig);
        }

        public void Clear()
        {
            BaseClear();
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new Group();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((Group)element).name;
        }

        public void Remove(Group serviceConfig)
        {
            BaseRemove(serviceConfig.name);
        }

        public void RemoveAt(int index)
        {
            BaseRemoveAt(index);
        }

        public void Remove(string name)
        {
            BaseRemove(name);
        }
    }


    public class MapConfigs : ConfigurationElementCollection
    {
        public MapConfigs()
        {
           
        }

        public MapConfig this[int index]
        {
            get { return (MapConfig)BaseGet(index); }
            set
            {
                if (BaseGet(index) != null)
                {
                    BaseRemoveAt(index);
                }
                BaseAdd(index, value);
            }
        }

        public void Add(MapConfig serviceConfig)
        {
            BaseAdd(serviceConfig);
        }

        public void Clear()
        {
            BaseClear();
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new MapConfig();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((MapConfig)element).mapName;
        }

        public void Remove(MapConfig serviceConfig)
        {
            BaseRemove(serviceConfig.mapName);
        }

        public void RemoveAt(int index)
        {
            BaseRemoveAt(index);
        }

        public void Remove(string name)
        {
            BaseRemove(name);
        }

        public static implicit operator List<object>(MapConfigs v)
        {
            throw new NotImplementedException();
        }
    }

    public class Solution : ConfigurationElement
    {
        public Solution() { }

        public Solution(string name)
        {
            Name = name;
            //ReportType = reportType;
        }

        [ConfigurationProperty("name", DefaultValue = "", IsRequired = true, IsKey = true)]
        public string Name
        {
            get { return (string)this["name"]; }
            set { this["name"] = value; }
        }



    }



}
