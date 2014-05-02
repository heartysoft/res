using System;
using System.Linq;
using System.Threading.Tasks;

namespace Res.Client
{
    public class ResClientEventPublisher : ResEventPublisher
    {
        private readonly string _context;
        private readonly ResClient _client;
        private readonly TypeTagResolver _typeTagResolver;
        private readonly Func<object, string> _serialiser;

        public ResClientEventPublisher(string context, ResClient client, TypeTagResolver typeTagResolver, Func<object, string> serialiser)
        {
            _context = context;
            _client = client;
            _typeTagResolver = typeTagResolver;
            _serialiser = serialiser;
        }

        public Task<CommitResponse> Publish(string stream, EventObject[] events, long expectedVersion = ExpectedVersion.Any, TimeSpan? timeout = null)
        {
            var toPublish = 
                events.Select(
                    x => x.ToEventData(_serialiser, _typeTagResolver))
                .ToArray();

            if (timeout.HasValue)
                return _client.CommitAsync(_context, stream, toPublish, expectedVersion, timeout.Value);

            return _client.CommitAsync(_context, stream, toPublish, expectedVersion);
        }
    }
}