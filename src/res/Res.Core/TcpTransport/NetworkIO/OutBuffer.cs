using System;
using System.Collections.Concurrent;

namespace Res.Core.TcpTransport.NetworkIO
{
    public class OutBuffer
    {
        readonly BlockingCollection<TaskCompleted> _completeds;

        public OutBuffer(int maxSizeAsPowerOfTwo)
        {
            //_completeds = new BlockingCollection<TaskCompleted>((int)Math.Pow(2, maxSizeAsPowerOfTwo));
            //This is for output, and not spcifying the size revert to ConcurrentQueue. 
            //Bounding slows down considerably. Should remove when ring array in place.
            _completeds = new BlockingCollection<TaskCompleted>(); 
        }

        public bool Offer(TaskCompleted completed)
        {
            return _completeds.TryAdd(completed);
        }

        public void OfferAndWaitUntilAccepted(TaskCompleted completed)
        {
            _completeds.Add(completed);
        }

        public bool Poll(out TaskCompleted completed)
        {
            return _completeds.TryTake(out completed);
        }
    }
}