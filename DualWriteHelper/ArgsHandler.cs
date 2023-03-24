// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using CommandLine;
using DWLibary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace DWHelper
{
    internal class ArgsHandler
    {
        public Options parsedOptions;
        public void parseCommands(string[] args)
        {
            CommandLine.Parser.Default.ParseArguments<Options>(args)
           .WithParsed<Options>(o => {
               // parsing successful; go ahead and run the app
               GlobalVar.username = o.username;
               GlobalVar.password = o.password;
               GlobalVar.foEnv = o.environment;
               GlobalVar.configFileName = o.configFileName;
               GlobalVar.mfasecret = o.mfasecret;
               GlobalVar.useadowikiupload= o.useadowikiupload;
               GlobalVar.adotoken = o.adotoken;

               if(o.status != null && o.status != String.Empty)
               {
                  // GlobalVar.exportConfig = true;

                   GlobalVar.exportState = DWEnums.GetValueFromDescription<DWEnums.MapStatus>(o.status);

               }

               if(o.runmode != null)
               {
                   DWEnums.RunMode parsed;
                   Enum.TryParse<DWEnums.RunMode>(o.runmode, out parsed);

                   GlobalVar.runMode = parsed;

                    


               }

               GlobalVar.noSolutions = o.noSolutions;

               parsedOptions = o;

               Console.WriteLine("Commandline arguments parsed and set");

            })
           .WithNotParsed<Options>(e => {
               // parsing unsuccessful; deal with parsing errors
               throw new Exception("Commandline parameters wrong");
               

            });
        }



    }

    class Options
    {
        [Option('u', "username", HelpText = "Username to use")]
        public string username { get; set; }

        [Option('p', "password", HelpText = "Password to use")]
        public string password { get; set; }

        [Option('e', "environment", HelpText = "Environment without https://www.")]
        public string environment { get; set; }

        [Option('c', "config", HelpText = "define what config to use for execution", Default = "")]
        public string configFileName { get; set; }

        [Option('s', Default ="", HelpText = "Status for export, values Running, All, Stopped")]
        public string status { get; set; }

        [Option('l', Default = "", HelpText = "Log level, values Debug, Information, Error")]
        public string logLevel { get; set; }

        [Option("nosolutions", Default =false, HelpText = "Prevents solutions from beeing applied")]
        public bool noSolutions { get; set; }

        [Option("runmode", HelpText = "Overwrites the runmode in the config file, possible: deployment, deployInitialSync, start, stop, pause, export")]
        public string runmode { get; set; }

        [Option("mfasecret", Default ="", HelpText = "Overwrites the mfasecret in the config file, usable in deployment pipelines when the secret is storerd in a key vault")]
        public string mfasecret { get; set; }

        [Option("useadowikiupload", Default = false, HelpText = "Overwrites the UseADOWikiUpload parameter in the config, usable in pipelines")]
        public bool useadowikiupload { get; set; }

        [Option("adotoken", Default ="", HelpText = "Overwrites the AccessToken in the config file for ADO Wiki uploads, usable in deployment pipelines when the secret is storerd in a key vault")]
        public string adotoken { get; set; }

    }
}
