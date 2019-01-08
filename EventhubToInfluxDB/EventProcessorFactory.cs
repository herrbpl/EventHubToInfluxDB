using Microsoft.Azure.EventHubs.Processor;
using System;
using System.Collections.Generic;
using System.Text;

namespace EventhubToInfluxDB
{
    class EventProcessorFactory: IEventProcessorFactory
    {
        private readonly MessageConverter _mg;
        public EventProcessorFactory(MessageConverter mg)
        {
            _mg = mg;
        }

        public IEventProcessor CreateEventProcessor(PartitionContext context)
        {
            return new EventProcessor(_mg);
        }
    }
}
