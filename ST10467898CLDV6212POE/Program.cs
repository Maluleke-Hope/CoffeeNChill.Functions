using System;
using Azure.Data.Tables;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication() // <--- THIS IS THE FIX
    .ConfigureServices(services =>
    {
        var storageConn = Environment.GetEnvironmentVariable("AzureWebJobsStorage") ?? "UseDevelopmentStorage=true";
        services.AddSingleton(new TableServiceClient(storageConn));
    })
    .Build();

host.Run();