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
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.EventHubs.Processor;

namespace EventhubToInfluxDB
{
    class Program
    {
        private static ManualResetEventSlim ended = new ManualResetEventSlim(false);
        private static ManualResetEventSlim startwait = new ManualResetEventSlim(false);

        private static IConfigurationRoot config;
        private readonly ILogger _logger;
        private readonly IServiceProvider _serviceProvider;  


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
                Console.ReadLine();
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
                        
        }


        public Program()
        {
            

            var serviceCollection = new ServiceCollection()
                .AddLogging(builder =>
                {
                    builder
                        .AddConfiguration(config.GetSection("Logging"))
                        /*.AddFilter("Microsoft", LogLevel.Warning)
                        .AddFilter("System", LogLevel.Warning)
                        .AddFilter("EventhubToInfluxDB.Program", LogLevel.Debug)*/
                        .AddConsole();

                });

            serviceCollection.AddOptions();            
            serviceCollection.Configure<InfluxOptions>(config.GetSection("InfluxDB").GetSection("Server"));
            serviceCollection.Configure<MessageConverterOptions>(config.GetSection("InfluxDB").GetSection("Measurement"));
            serviceCollection.AddScoped<InfluxInjector>();
            serviceCollection.AddScoped<MessageConverter>();
            serviceCollection.AddScoped<EventProcessorFactory>();
            


            _serviceProvider = serviceCollection.BuildServiceProvider();
            // getting the logger using the class's name is conventional
            _logger = _serviceProvider.GetRequiredService<ILogger<Program>>();
          
            
        }

        private async Task RunServerAsync(string[] args)
        {            
            string EventHubConnectionString = config["EventHub:ConnectionString"];
            string EventHubName = config["EventHub:Name"];
            string EventHubConsumerGroupName = config["EventHub:ConsumerGroup"];
            string StorageConnectionString = config["Storage:ConnectionString"];
            string StorageContainer = config["Storage:Container"];

            /*
            string MeasurementName = config["InfluxDb:Measurement:Name"];
            string TimestampName = config["InfluxDb:Measurement:Timestamp"];

            var tags = new List<string>();
            var fields = new List<string>();

            config.Bind("InfluxDb:Measurement:Tags", tags);
            config.Bind("InfluxDb:Measurement:Fields", fields);
            */

            using (var scope = _serviceProvider.CreateScope())
            {
                EventProcessorFactory eventProcessorFactory = scope.ServiceProvider.GetRequiredService<EventProcessorFactory>();

                try
                {

                    //MessageConverter mc = new MessageConverter(MeasurementName, TimestampName, tags, fields);
                    //EventProcessorFactory eventProcessorFactory = new EventProcessorFactory(mc);
                    
                    var eventProcessorHost = new EventProcessorHost(
                        EventHubName,
                        EventHubConsumerGroupName,
                        EventHubConnectionString,
                        StorageConnectionString,
                        StorageContainer);

                    var _eventProcessorOptions = new EventProcessorOptions();

                    // Registers the Event Processor Host and starts receiving messages
                    await eventProcessorHost.RegisterEventProcessorFactoryAsync(eventProcessorFactory, _eventProcessorOptions);

                    //await eventProcessorHost.RegisterEventProcessorAsync<EventProcessor>();
                    _logger.LogInformation("Started");

                    startwait.Wait();
                    _logger.LogInformation("Stopping");

                    await eventProcessorHost.UnregisterEventProcessorAsync();
                    _logger.LogInformation("Stopped");

                }
                catch (Exception e)
                {
                    startwait.Set();
                    _logger.LogError($"{e}");
                }
                finally
                {
                    ended.Set();
                }
            }
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
                "Storage:Container",
                "InfluxDb:Measurement:Name",
                "InfluxDb:Measurement:Timestamp",
                "InfluxDb:Measurement:Fields:0"
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
