using System;
using System.Collections.Generic;
using NetMQ;

namespace Res.Client.Internal.NetMQ
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

        public static long PopInt64(this NetMQMessage msg)
        {
            return BitConverter.ToInt64(msg.Pop().Buffer, 0);
        }

        public static long? PopNullableInt64(this NetMQMessage msg)
        {
            var buffer = msg.Pop().Buffer;
            if (buffer.Length == 0) return null;

            return BitConverter.ToInt64(buffer, 0);
        }

        public static int PopInt32(this NetMQMessage msg)
        {
            return BitConverter.ToInt32(msg.Pop().Buffer, 0);
        }

        public static Guid PopGuid(this NetMQMessage msg)
        {
            return new Guid(msg.Pop().Buffer);
        }

        public static DateTime PopDateTime(this NetMQMessage msg)
        {
            return DateTime.FromBinary(PopInt64(msg));
        }

        public static NetMQFrame ToNetMqFrame(this DateTime dateTime)
        {
            return new NetMQFrame(BitConverter.GetBytes(dateTime.ToBinary()));
        }

        public static NetMQFrame ToNetMqFrame(this long value)
        {
            return new NetMQFrame(BitConverter.GetBytes(value));
        }

        public static NetMQFrame ToNetMqFrame(this int value)
        {
            return new NetMQFrame(BitConverter.GetBytes(value));
        }

        public static NetMQFrame ToNetMqFrame(this int? value)
        {
            return value.HasValue ? new NetMQFrame(BitConverter.GetBytes(value.Value)) : NetMQFrame.Empty;
        }
        public static NetMQFrame ToNetMqFrame(this long? value)
        {
            return value.HasValue ? new NetMQFrame(BitConverter.GetBytes(value.Value)) : NetMQFrame.Empty;
        }
    }
}