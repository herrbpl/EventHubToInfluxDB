using Microsoft.Azure.EventHubs.Processor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace EventhubToInfluxDB
{
    class EventProcessorFactory: IEventProcessorFactory
    {
        private readonly MessageConverter _mc;
        private readonly InfluxInjector _ii;
        private readonly ILoggerFactory _loggerFactory;
        public EventProcessorFactory(ILoggerFactory loggerFactory, MessageConverter messageConverter, InfluxInjector influxInjector)
        {
            _mc = messageConverter ?? throw new ArgumentNullException();
            _ii = influxInjector ?? throw new ArgumentNullException();
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException();
        }

        public IEventProcessor CreateEventProcessor(PartitionContext context)
        {
            
            return new EventProcessor(_loggerFactory.CreateLogger<EventProcessor>(),_mc, _ii);
        }
    }
}
