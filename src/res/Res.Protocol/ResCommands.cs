using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Res.Protocol
{
    public static class ResCommands
    {
        public const string AppendCommit = "AC";
        public const string CommitResult = "CR";
        public const string Error = "ER";
        public const string SubscribeToQueue = "SQ";
        public const string QueuedEvents = "QE";
        public const string AcknowledgeQueue = "AQ";
        public const string QueryEventsByStream = "QS";
        public const string QueryEventsByStreamResponse = "QR";
    }
}
