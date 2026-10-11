using Azure.Data.Tables;
using Azure.Storage.Queues;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ST10467898CLDV6212POE
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults()
                .ConfigureServices((context, services) =>
                {
                    string connectionString =
                        context.Configuration["AzureWebJobsStorage"]
                        ?? throw new InvalidOperationException(
                            "AzureWebJobsStorage is not configured.");

                    services.AddSingleton(new TableServiceClient(connectionString));
                    services.AddSingleton(new QueueServiceClient(connectionString));
                })
                .Build();

            host.Run();
        }
    }
}