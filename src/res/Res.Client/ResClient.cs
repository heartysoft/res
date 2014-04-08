using System.CodeDom;
using System.Threading.Tasks;

namespace Res.Client
{
    public interface ResClient
    {
        Task<CommitResponse> CommitAsync(string context, string stream, EventData[] events, long expectedVersion);
    }
}