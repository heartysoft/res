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
        public const string ResultReady = "RR";
        public const string CommitResult = "CR";
        public const string Error = "ER";
        public const string RegisterSubscriptions = "RS";
        public const string SubscribeResponse = "SR";
        public const string FetchEvents = "FE";
        public const string EventsFetched = "EF";
        public const string ProgressSubscriptions = "PS";
        public const string SubscriptionsProgressed = "SP";
        public const string SetSubscriptions = "SS";
        public const string SubscriptionsSet = "ST";
    }
}
