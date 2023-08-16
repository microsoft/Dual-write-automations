using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;

namespace DWLibary
{
    public class ArgsHandler
    {
        public Options parsedOptions;

        public void parseCommands(string[] args)
        {

            var parser = new CommandLine.Parser(s =>
            {
                s.CaseSensitive = false;
                s.CaseInsensitiveEnumValues = true;
            });
            parser.ParseArguments<Options>(args)
           .WithParsed<Options>(o => {
               // parsing successful; go ahead and run the app
               GlobalVar.username = o.username;
               GlobalVar.password = o.password;
               GlobalVar.foEnv = o.environment;
               GlobalVar.configFileName = o.configFileName;
               GlobalVar.mfasecret = o.mfasecret;
               GlobalVar.useadowikiupload = o.useadowikiupload;
               GlobalVar.adotoken = o.adotoken;

               GlobalVar.runMode = o.runmode;
               GlobalVar.exportState = o.status;
               GlobalVar.exportOption = o.exportOption;

               GlobalVar.noSolutions = o.noSolutions;

               GlobalVar.newConfigFileName = o.newConfigFileName;

               parsedOptions = o;

               GlobalVar.parsedOptions = o;

               Console.WriteLine("Commandline arguments parsed and set");

           })
           .WithNotParsed<Options>(e => {
               // parsing unsuccessful; deal with parsing errors
               throw new Exception("Commandline parameters wrong");


           });
        }



    }
}
