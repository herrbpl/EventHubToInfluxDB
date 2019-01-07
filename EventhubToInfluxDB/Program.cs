using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Logging.Debug;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;

namespace EventhubToInfluxDB
{
    class Program
    {
        private static ManualResetEventSlim ended = new ManualResetEventSlim(false);
        private static ManualResetEventSlim startwait = new ManualResetEventSlim(false);

        private static IConfigurationRoot config;
        private readonly ILogger _logger;


        static void Main(string[] args)
        {

            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var confbuilder = new ConfigurationBuilder()
               .SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
               .AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: true)
               .AddEnvironmentVariables("EH2TI_")
               .AddCommandLine(args);

            config = confbuilder.Build();

            var configCheck = CheckConfig(config);

            if (configCheck.Count > 0)
            {
                Console.WriteLine("Errors while loading configuration:");
                foreach (var item in configCheck)
                {
                    Console.WriteLine($"'{item.Key}' - {item.Value}");
                }
                Console.WriteLine("Exiting.");
                return;
            }


            AssemblyLoadContext.Default.Unloading += ctx =>
            {
                startwait.Set();
                System.Console.WriteLine("Waiting for main thread shutdown");
                ended.Wait();
            };

            Console.CancelKeyPress += (object sender, ConsoleCancelEventArgs e) =>
            {
                // start waiting app end
                startwait.Set();
                e.Cancel = true;
            };

            var program = new Program();

            Task.Run(async () => await program.RunServerAsync(args));

            //RunServerAsync(args).Wait();
            ended.Wait();
            System.Console.WriteLine("Shutdown complete. Exiting.");
            Console.ReadLine();
        }


        public Program()
        {
            

            var serviceCollection = new ServiceCollection()
                .AddLogging(builder =>
                {
                    builder
                        .AddConfiguration(config.GetSection("Logging"))
                        .AddFilter("Microsoft", LogLevel.Warning)
                        .AddFilter("System", LogLevel.Warning)
                        .AddFilter("EventhubToInfluxDB.Program", LogLevel.Debug)
                        .AddConsole();

                });

            var serviceProvider = serviceCollection.BuildServiceProvider();
            // getting the logger using the class's name is conventional
            _logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        }

        private async Task RunServerAsync(string[] args)
        {
            
            string EventHubConnectionString = config["EventHub:ConnectionString"];
            string EventHubName = config["EventHub:Name"];
            string EventHubConsumerGroupName = config["EventHub:ConsumerGroup"];
            string StorageConnectionString = config["Storage:ConnectionString"];
            string StorageContainer = config["Storage:Container"];
            Console.WriteLine("Started.");
            startwait.Wait();
            Console.WriteLine("Stopping.");

            ended.Set();
            Console.WriteLine("Stopped.");
        }



        // check that important parts of configuration are existing
        private static Dictionary<string, string> CheckConfig(IConfigurationRoot config)
        {
            var result = new Dictionary<string, string>();

            List<string> requiredConfigValues = new List<string>()
            {
                "EventHub:ConnectionString",
                "EventHub:Name",
                "EventHub:ConsumerGroup",
                "Storage:ConnectionString",                
                "Storage:Container"                
            };

            var lookup = config.AsEnumerable().ToDictionary((i) => { return i.Key; });
            foreach (var item in requiredConfigValues)
            {

                if (!lookup.ContainsKey(item))
                {
                    result.Add(item, "is missing!");
                }
                else if (lookup[item].Value == null || lookup[item].Value == "")
                {
                    result.Add(item, "has no value defined!");
                }
            }
            return result;
        }

    }
}
