using Azure.Data.Tables;
using Azure.Storage.Files.Shares;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ST10467898CLDV6212POE;

public class Program
{
    public static void Main(string[] args)
    {
        var host = new HostBuilder()
            .ConfigureFunctionsWebApplication()
            .ConfigureServices((context, services) =>
            {
                // Read the Azure Storage connection string from configuration.
                string connectionString =
                    context.Configuration["AzureWebJobsStorage"]
                    ?? throw new InvalidOperationException(
                        "AzureWebJobsStorage is not configured.");

                // Register the Azure Table Storage client.
                services.AddSingleton(
                    new TableServiceClient(connectionString));

                // Register the Azure File Share client.
                services.AddSingleton(
                    new ShareServiceClient(connectionString));
            })
            .Build();

        host.Run();
    }
}