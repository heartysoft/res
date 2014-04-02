namespace Res.Core.Storage
{
    public class CommitsForStorage
    {
        public CommitForStorage[] Commits { get; private set; }

        public CommitsForStorage(params CommitForStorage[] commits)
        {
            Commits = commits;
        }
    }
}