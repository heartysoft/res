using System;
using System.Linq;
using System.Threading.Tasks;
using Res.Protocol;

namespace Res.Client
{
    public class ResClientEventPublisher : ResEventPublisher
    {
        private readonly string _context;
        private readonly ResPublisher _publisher;
        private readonly TypeTagResolver _typeTagResolver;
        private readonly Func<object, string> _serialiser;

        public ResClientEventPublisher(string context, ResPublisher publisher, TypeTagResolver typeTagResolver, Func<object, string> serialiser)
        {
            _context = context;
            _publisher = publisher;
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
                return _publisher.CommitAsync(_context, stream, toPublish, expectedVersion, timeout.Value);

            return _publisher.CommitAsync(_context, stream, toPublish, expectedVersion);
        }
    }
}