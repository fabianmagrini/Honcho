using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text;
using Honcho.Core.Services;
using Honcho.Core.Interfaces;
using Honcho.Core.FileLock;
 
namespace Client
{
    public class App
    {
        private readonly ILogger<App> _logger;
        private readonly ILoggerFactory _factory;
        private readonly IConfigurationRoot _config;
 
        public App(ILoggerFactory factory, IConfigurationRoot config)
        {
            _factory = factory;
            _logger = _factory.CreateLogger<App>();
            _config = config;
        }
 
        public void Run()
        {
            LeaderElectionService electionService = new LeaderElectionService(Lock.Create("services/leader", new TimeSpan(0, 0, 0, 10)), _factory.CreateLogger<LeaderElectionService>());
            electionService.LeaderChanged += ((source,arguments) => 
            {
                if (arguments.IsLeader) 
                {
                    _logger.LogInformation("Active");
                } 
                else
                {
                    _logger.LogInformation("Standby");
                }
            });
            electionService.Start();
            Console.ReadLine();
        }
    }
}