using System;
using System.Net;
using Grains;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Hosting;
using Orleans.Runtime.Configuration;
using static Orleans.Runtime.Configuration.GlobalConfiguration;

namespace Silo
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = new ClusterConfiguration
            {
                Defaults =
                {
                    ProxyGatewayEndpoint = new IPEndPoint(IPAddress.Any, 50000),
                    HostNameOrIPAddress = string.Empty,
                    Port = 60000
                }
            };

            var azureConnection = "UseDevelopmentStorage=true";

            config.AddAzureTableStorageProvider("Default", connectionString: azureConnection);

            config.RegisterDashboard();

            config.Globals.DeploymentId = "main";
            config.Globals.DataConnectionString = azureConnection;
            config.Globals.LivenessType = LivenessProviderType.AzureTable;
            config.Globals.ReminderServiceType = ReminderServiceProviderType.AzureTable;

            var builder = new SiloHostBuilder()
                .UseConfiguration(config)
                .UseAzureTableMembership(options => options.ConnectionString = azureConnection)
                .UseDashboard(options =>
                {
                    options.HostSelf = true;
                    options.HideTrace = true;
                })
                .ConfigureLogging(logging => logging.AddConsole())
                .AddApplicationPart(typeof(GameGrain).Assembly)
                .AddApplicationPartsFromReferences(typeof(GameGrain).Assembly);

            var host = builder.Build();
            host.StartAsync().Wait();

            Console.ReadKey();

            host.StopAsync().Wait();
        }
    }
}
