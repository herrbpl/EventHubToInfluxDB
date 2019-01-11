using Microsoft.Azure.EventHubs;
using Microsoft.Azure.EventHubs.Processor;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventhubToInfluxDB
{
    class EventProcessor: IEventProcessor
    {
        private readonly MessageConverter _mg;
        private readonly InfluxInjector _ii;
        private ILogger _logger;

        public EventProcessor(ILogger<EventProcessor> logger, MessageConverter mg, InfluxInjector influxInjector)
        {
            _logger = logger ?? throw new ArgumentNullException();
            _mg = mg ?? throw new ArgumentNullException();
            _ii = influxInjector ?? throw new ArgumentNullException();
            Console.WriteLine("EventProcessor created!");
        }

        public Task CloseAsync(PartitionContext context, CloseReason reason)
        {
            Console.WriteLine($"Processor Shutting Down. Partition '{context.PartitionId}', Reason: '{reason}'.");
            return Task.CompletedTask;
        }

        public Task OpenAsync(PartitionContext context)
        {
            Console.WriteLine($"SimpleEventProcessor initialized. Partition: '{context.PartitionId}'");
            return Task.CompletedTask;
        }

        public Task ProcessErrorAsync(PartitionContext context, Exception error)
        {
            Console.WriteLine($"Error on Partition: {context.PartitionId}, Error: {error.Message}");
            return Task.CompletedTask;
        }


        public async Task ProcessEventsAsync(PartitionContext context, IEnumerable<EventData> messages)
        {
            foreach (var eventData in messages)
            {

                eventData.SystemProperties.AsEnumerable<KeyValuePair<string, object>>().ToDictionary(t=> t.Key, t => t.Value);

                var data = Encoding.UTF8.GetString(eventData.Body.Array, eventData.Body.Offset, eventData.Body.Count);
                //Console.WriteLine($"Message received. Partition: '{context.PartitionId}', Data: '{data}'");

                // if message processing fails, for example, if influxdb is down, message will not be delivered. 
                // to eliminate that, internal queue should be used for forage and processing. 

                var result = _mg.Convert(data, null, null);                

                if (result != null)
                {
                    //Console.WriteLine(result);
                    _logger.LogDebug($"Injecting {result}");
                    await _ii.InjectAsync(result);
                }

                /*var jsonstr = JsonConvert.SerializeObject(eventData, Formatting.Indented);
                Console.WriteLine($"Message received. Partition: '{context.PartitionId}', Event: '{jsonstr}'");
                */

            }
            
            await context.CheckpointAsync();
        }
    }
}
