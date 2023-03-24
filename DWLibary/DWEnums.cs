// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DWLibary
{
    public class DWEnums
    {
        public enum ExecutionMode
        {
            parallel,
            sequential
        };

        public enum StartStop
        {
            none,
            start,
            stop,
            pause,
            resume,
            init = 8
        };

        public enum MapStatus
        {
            None = 0,
            Stopped = 1,
            InitialSync = 2,
            
            Running = 4,
            Paused = 5,
            //Paused = 4,
            NotRunning = 6,
            Keep = 99
        };

        public enum RequestStatus
        {
            Waiting = 1,
            Completed = 2,
            Error = 3
        };

        public enum ExceptionHandling
        {
            ignore,
            skip,
            stop

        };

        public enum DWSyncDirection
        {
            [Description("FO <-> CE")]
            Both = 3,
            [Description("FO -> CE")]
            FOOnly = 1,
            [Description("FO <- CE")]
            CEOnly = 2
        }

        public enum DataMaster
        {
            [Description("CRM")]
            CE = 0,
            [Description("AX")]
            FO = 1
            
        };

        public enum RunMode
        {
            [Description("Default from configuration")]
            none,
            [Description("Deploy maps")]
            deployment,
            [Description("Deploy and Initial sync where set in config")]
            deployInitialSync,
            [Description("Start maps")]
            start,
            [Description("Stop maps")]
            stop,
            [Description("Pause maps")]
            pause,
            [Description("Export configuration")]
            export
        };


        public static T GetValueFromDescription<T>(string description) where T : Enum
        {
            foreach (var field in typeof(T).GetFields())
            {
                if (Attribute.GetCustomAttribute(field,
                typeof(DescriptionAttribute)) is DescriptionAttribute attribute)
                {
                    if (attribute.Description == description)
                        return (T)field.GetValue(null);
                }
                else
                {
                    if (field.Name == description)
                        return (T)field.GetValue(null);
                }
            }

            //throw new ArgumentException("Not found.", nameof(description));
            return default(T);
        }
        public static string DescriptionAttr<T>(T source)
        {
            FieldInfo fi = source.GetType().GetField(source.ToString());

            DescriptionAttribute[] attributes = (DescriptionAttribute[])fi.GetCustomAttributes(
                typeof(DescriptionAttribute), false);

            if (attributes != null && attributes.Length > 0) return attributes[0].Description;
            else return source.ToString();
        }

    }
}
