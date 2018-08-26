using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System;
using System.IO;

namespace Client
{    class Program
    {
        static void Main(string[] args)
        {
            // Create service collection
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
 
            // Create service provider
            var serviceProvider = serviceCollection.BuildServiceProvider();
 
            // Run app
            serviceProvider.GetService<App>().Run();
        }
 
        private static void ConfigureServices(IServiceCollection serviceCollection)
        {
            // Build configuration
            var configuration = new ConfigurationBuilder()
                //.SetBasePath(AppContext.BaseDirectory)
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", false)
                .Build();
 
            // Add console logging
            serviceCollection.AddLogging(factory => 
            {
                factory.AddConsole();
                factory.AddSerilog();
                factory.AddDebug();
            });
 
            // Add Serilog logging           
            Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .WriteTo.RollingFile(configuration["Serilog:LogFile"])
            .CreateLogger();
 
            // Add access to generic IConfigurationRoot
            serviceCollection.AddSingleton(configuration);
 
            // Add the App
            serviceCollection.AddTransient<App>();
        }
    }
}
