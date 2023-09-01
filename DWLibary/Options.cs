using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DWLibary
{

    public class Options
    {
        [Option('u', "username", HelpText = "Username to use")]
        public string username { get; set; }

        [Option('p', "password", HelpText = "Password to use")]
        public string password { get; set; }

        [Option('e', "environment", HelpText = "Environment without https://www.")]
        public string environment { get; set; }

        [Option('c', "config", HelpText = "define what config to use for execution", Default = "")]
        public string configFileName { get; set; }

        [Option('n', "new config", HelpText = "Define the name of the new configuration file during export", Default = "")]
        public string newConfigFileName { get; set; }

        [Option('s', Default = DWEnums.MapStatus.None, HelpText = "Status for export, values Running, All, Stopped")]
        public DWEnums.MapStatus status { get; set; }

        [Option('l', Default = "", HelpText = "Log level, values Debug, Information, Error")]
        public string logLevel { get; set; }

        [Option('t', "target environment", HelpText = "Target environment for comparison without https://www.")]
        public string targetenvironment { get; set; }

        [Option("nosolutions", Default = false, HelpText = "Prevents solutions from beeing applied")]
        public bool noSolutions { get; set; }

        [Option("forceReset", Default = false, HelpText = "Forces reset when using ResetLink")]
        public bool forceReset { get; set; }

        [Option("runmode", Default = DWEnums.RunMode.none, HelpText = "Overwrites the runmode in the config file, possible: deployment, deployInitialSync, start, stop, pause, export")]
        public DWEnums.RunMode runmode { get; set; }

        [Option("mfasecret", Default = "", HelpText = "Overwrites the mfasecret in the config file, usable in deployment pipelines when the secret is storerd in a key vault")]
        public string mfasecret { get; set; }

        [Option("useadowikiupload", Default = false, HelpText = "Overwrites the UseADOWikiUpload parameter in the config, usable in pipelines")]
        public bool useadowikiupload { get; set; }

        [Option("adotoken", Default = "", HelpText = "Overwrites the AccessToken in the config file for ADO Wiki uploads, usable in deployment pipelines when the secret is storerd in a key vault")]
        public string adotoken { get; set; }

        [Option('o', Default = DWEnums.ExportOptions.Default, HelpText = "Additional options for export")]
        public DWEnums.ExportOptions exportOption { get; set; }

    }
}
