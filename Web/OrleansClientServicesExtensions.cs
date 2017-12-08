using System;
using System.Threading.Tasks;
using GrainInterfaces;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Orleans.Runtime.Configuration;

namespace Web
{
    public static class OrleansClientServicesExtensions
    {
        public static void AddOrleansClient(this IServiceCollection services)
        {
            var config = new ClientConfiguration
            {
                DataConnectionString = "UseDevelopmentStorage=true",
                GatewayProvider = ClientConfiguration.GatewayProviderType.AzureTable,
                DeploymentId = "main"
            };

            IClusterClient client;
            var attempts = 5;
            while (true)
            {
                client = new ClientBuilder()
                    .UseConfiguration(config)
                    .AddApplicationPart(typeof(IGameGrain).Assembly).Build();
                try
                {
                    client.Connect().Wait();
                    break;
                }
                catch (Exception e)
                {
                    client.Dispose();
                    if (attempts == 0)
                    {
                        Console.WriteLine(e);
                        throw;
                    }
                    attempts--;
                    Task.Delay(4000).Wait();
                }
            }

            services.AddSingleton<IClusterClient>(client);
        }
    }
}