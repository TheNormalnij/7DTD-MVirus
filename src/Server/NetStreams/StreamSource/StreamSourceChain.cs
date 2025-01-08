
using MVirus.Shared.NetStreams;

namespace MVirus.Server.NetStreams
{
    public class StreamSourceChain : IStreamSource
    {
        private readonly IStreamSource[] chain;

        public StreamSourceChain(IStreamSource[] _chain)
        {
            chain = _chain;
        }

        public RequestedStreamParams CreateStream(string name)
        {
            foreach (var item in chain)
            {
                try
                {
                    return item.CreateStream(name);
                }
                catch {}
            }

            throw new NetStreamException(StreamErrorCode.NOT_FOUND);
        }
    }
}
