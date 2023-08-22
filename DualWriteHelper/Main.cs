// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

// See https://aka.ms/new-console-template for more information
using CommandLine;
using DWLibary;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using DWHelper;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Services.Common.CommandLine;
using Serilog;
using Microsoft.Extensions.Logging.ApplicationInsights;



#if DEBUG

//args = new string[] {"-u", "username/clientId", "-p" , "password/secret",
//"-e", "environment URL",
//"-c", "configFile.config",
//"-t", "tenant",  //not used yet
//"-g", "gateway", //not used yet
//"--export", "Running",
//"--nosolutions"
//};



using (StreamReader sr = new StreamReader(@"DEBUGArgs.txt"))
{
    
    var data = sr.ReadToEnd();

    data = data.Replace("\r", "");
    data = data.Replace("\n", "");

    string exp = @"((?:|^\b|\s+)--(?<option_1>.+?)(?:\s|=|$)(?!-)(?<value_1>[\""\'].+?[\""\']|.+?(?:\s|$))?|(?:|^\b)-(?<option_2>.)(?:\s|=|$)(?!-)(?<value_2>[\""\'].+?[\""\']|.+?(?:\s|$))?|(?<arg>[\""\'].+?[\""\']|.+?(?:\s|$)))";

    MatchCollection collection = Regex.Matches(data, exp);
    List<string> argsList = new List<string>();

    foreach(Match match in collection)
    
    {
        string value = match.Value.Trim();
        value = value.Replace("\"", "");
        argsList.Add(value);

        if (value == "--runmode")
            argsList.Add("ResetLink");

    }


    if (argsList.Count > 0)
    {
        args = new string[argsList.Count];

        argsList.CopyTo(args, 0);
    }


}



#endif





ArgsHandler argsHandler = new ArgsHandler();
argsHandler.parseCommands(args);


LogLevel level = LogLevel.Information;

if(argsHandler.parsedOptions.logLevel != null && argsHandler.parsedOptions.logLevel != "")
    Enum.TryParse<LogLevel>(argsHandler.parsedOptions.logLevel, out level);



Console.WriteLine($"LogLevel {level}");
GlobalVar.initConfig();

CreateHostBuilderv2(args, level).Build().Run();





static IHostBuilder CreateHostBuilderv2(string[] args, LogLevel _logLevel) =>
    Host.CreateDefaultBuilder(args).ConfigureLogging((hostingContext, builder) => {
        string subFolder = "Logs";
        string fileName = $"-{DateTime.Now.ToString("yyyy-MM-dd")}_{GlobalVar.foEnv}-{new Random().Next(1, 99999999)}.txt";

        if (_logLevel <= LogLevel.Debug)
            builder.AddFile(Path.Combine(subFolder, "DEBUG" + fileName), _logLevel);

        builder.AddFile(Path.Combine(subFolder, "ERROR" + fileName), LogLevel.Error);
        builder.AddFile(Path.Combine(subFolder, "WARN" + fileName), LogLevel.Warning);

        builder.AddFile(Path.Combine(subFolder, "LOG-" + fileName), LogLevel.Information).SetMinimumLevel(LogLevel.Information);

        string appInsightConStr = String.Empty;
        try
        {
            appInsightConStr =  GlobalVar.config.AppSettings.Settings["appInsightConnectionString"].Value;
        }
        catch 
        {
            Console.WriteLine("No app Insights connection string found!");
        }

        if (appInsightConStr != null && appInsightConStr != "")
        {
            builder.AddApplicationInsights(configureTelemetryConfiguration: (config) =>
                config.ConnectionString = GlobalVar.config.AppSettings.Settings["appInsightConnectionString"].Value,
                

                configureApplicationInsightsLoggerOptions: (options) => { }
                );
        }
    })
        .ConfigureServices((_, services) =>
            services.AddHostedService<DWHostedService>());