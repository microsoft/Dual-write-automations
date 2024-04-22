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

               GlobalVar.foEnv = parseUriHostname(o.environment);
               
               
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

               

               o.targetenvironment = parseUriHostname(o.targetenvironment);
               GlobalVar.parsedOptions = o;


               Console.WriteLine("Commandline arguments parsed and set");

           })
           .WithNotParsed<Options>(e => {

               foreach (var o in e)
               {
                   Console.WriteLine("Not parsed parameter" + o.ToString());
               }
               // parsing unsuccessful; deal with parsing errors
               throw new Exception("Commandline parameters wrong");


           });
        }


        private string parseUriHostname(string url)
        {

            string ret = String.Empty;

            if(url == null || url.Length == 0)
            {
                return ret;
            }

            url = url.Trim();

            UriBuilder builder = new UriBuilder(url);
            if (builder.Uri != null)
            {
                ret = builder.Uri.Host;
            }
            

            return ret;


        }



    }
}
