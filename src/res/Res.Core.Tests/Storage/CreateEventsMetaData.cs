using System;

namespace Res.Core.Tests.Storage
{
    public class CreateEventsMetaData
    {
        public int NumberOfEvents { get; set; }
        public string Context { get; set; }
        public string Stream { get; set; }
        public DateTime[] SameTimeStamps { get; set; }

        public CreateEventsMetaData(int numberOfEvents,string context,string stream,DateTime[] sameTimeStamps)
        {
            NumberOfEvents = numberOfEvents;
            Context = context;
            Stream = stream;
            SameTimeStamps = sameTimeStamps;
        }
    }
}