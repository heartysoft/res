using System.Collections.Generic;
using NetMQ;

namespace Res.Core.TcpTransport
{
    public static class NetMqExtensions
    {
        public static NetMQFrame[] PopUntilEmptyFrame(this NetMQMessage message)
        {
            var list = new List<NetMQFrame>(message.FrameCount);
            while (true)
            {
                var frame = message.Pop();
                if (frame.BufferSize == 0)
                    break;

                list.Add(frame);
            }

            return list.ToArray();
        }

        public static void Append(this NetMQMessage message, NetMQFrame[] frames)
        {
            foreach (var frame in frames)
                message.Append(frame);
        }
    }
}