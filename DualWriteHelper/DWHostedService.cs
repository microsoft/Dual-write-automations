// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using DWLibary;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Configuration;

namespace DWHelper;

public class DWHostedService : IHostedService
{
    private readonly ILogger _logger;

    private readonly IHostApplicationLifetime _appLifetime;

    public bool isRunning { get; set; }
    public DWHostedService(
        ILogger<DWHostedService> logger,
        IHostApplicationLifetime appLifetime)
    {
        _logger = logger;
        _appLifetime = appLifetime;
        appLifetime.ApplicationStarted.Register(OnStarted);
        appLifetime.ApplicationStopping.Register(OnStopping);
        appLifetime.ApplicationStopped.Register(OnStopped);
    }

    private void DoWorkAsync()
    {
        _logger.LogInformation($"Background Service is working.  {DateTime.Now}");
        try
        {
            isRunning = true;
            AppExecution ae = new AppExecution(_logger, _appLifetime);

            ae.run();
            // dosometing you want
        }
        catch (Exception ex)
        {
            isRunning = false;
            _logger.LogInformation("Error {0}", ex.Message);

            throw ex;

        }
       // return Task.CompletedTask;
    }
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting...");


        //start new thread
        Thread t = new Thread(new ThreadStart(DoWorkAsync));
        t.Start();
        //DoWorkAsync(null);



        //StopAsync(cancellationToken);


        return Task.CompletedTask;
    }





    public Task StopAsync(CancellationToken cancellationToken)
    {
       // _logger.LogInformation("4. StopAsync has been called.");

        return Task.CompletedTask;
    }


    private void OnStarted()
    {

        //_logger.LogInformation("2. OnStarted has been called.");
    }

    private void OnStopping()
    {
       // _logger.LogInformation("3. OnStopping has been called.");
        
    }

    private void OnStopped()
    {

        int errorCode = 0;

        if (GlobalVar.errors.Count > 0)
            errorCode = 400;

        if(errorCode == 0)
            _logger.LogInformation($"Application stopped successful without errors!");
        else
            _logger.LogInformation($"Application stopped with errors, check the log files!");

        Environment.Exit(errorCode);
    }
}
