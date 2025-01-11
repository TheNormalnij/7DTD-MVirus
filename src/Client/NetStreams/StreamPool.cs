
using MVirus.Shared.NetStreams;
using System;

namespace MVirus.Client.NetStreams
{
    internal class StreamPool
    {
        private const byte STREAM_POOL_SIZE = 16;
        private const byte MAX_STREAM_ID = 255;
        private readonly NetStreamRequest[] activeRequests = new NetStreamRequest[STREAM_POOL_SIZE];
        private byte lastStreamId = 0;

        public byte Add(NetStreamRequest req)
        {
            var streamId = GetNextStreamId();

            req.streamId = streamId;
            activeRequests[streamId & 15] = req;

            return streamId;
        }

        public NetStreamRequest Get(byte streamId)
        {
            var req = activeRequests[streamId & 15];
            if (req == null || req.streamId != streamId)
                return null;
            return req;
        }

        public void Remove(byte streamId)
        {
            var pos = streamId & 15;
            var req = activeRequests[pos];
            if (req == null || req.streamId != streamId)
                return;
            activeRequests[pos] = null;
        }

        public bool IsEmpty()
        {
            foreach (var req in activeRequests)
            {
                if (req != null)
                    return false;
            }
            return true;
        }

        public void Foreach(Action<NetStreamRequest> action)
        {
            foreach (var item in activeRequests)
            {
                if (item != null)
                    action(item);
            }
        }

        private byte GetNextStreamId()
        {
            var lastUsedSlot = lastStreamId & 15;
            var nextSlot = GetFreeSlot((byte)lastUsedSlot);
            var mult = lastStreamId / STREAM_POOL_SIZE;
            var nextStreamId = nextSlot + (mult * STREAM_POOL_SIZE);
            if (nextStreamId <= lastStreamId)
                nextStreamId += STREAM_POOL_SIZE;

            if (nextStreamId > MAX_STREAM_ID)
                nextStreamId -= MAX_STREAM_ID;

            lastStreamId = (byte)nextStreamId;

            return lastStreamId;
        }

        private byte GetFreeSlot(byte fromSlot)
        {
            for (int i = fromSlot + 1; i < STREAM_POOL_SIZE; i++)
            {
                if (activeRequests[i] == null)
                {
                    return (byte)i;
                }
            }
            for (int i = 0; i < fromSlot; i++)
            {
                if (activeRequests[i] == null)
                {
                    return (byte)i;
                }
            }

            throw new NetStreamException("Stream limit reached");
        }
    }
}
