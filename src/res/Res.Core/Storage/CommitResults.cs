using System;

namespace Res.Core.Storage
{
    public class CommitResults
    {
        public Guid[] SuccessfulCommits { get; private set; }
        public Guid[] FailedDueToConcurrencyCommits { get; private set; }

        public CommitResults(Guid[] successfulCommits, Guid[] failedDueToConcurrencyCommits)
        {
            SuccessfulCommits = successfulCommits;
            FailedDueToConcurrencyCommits = failedDueToConcurrencyCommits;
        }
    }
}