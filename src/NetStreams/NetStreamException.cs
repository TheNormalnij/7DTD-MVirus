using System;

namespace MVirus.NetStreams
{
    public enum StreamErrorCode
    {
        NOT_FOUND,
        NOT_SUPPORTED,
        INVALID_STATE,
        UNKNOWN_ERROR,
    }

    public class NetStreamException : Exception
    {
        public StreamErrorCode ErrorCode { get; private set; }

        public NetStreamException(string message) : base(message) {
            ErrorCode = StreamErrorCode.UNKNOWN_ERROR;
        }

        public NetStreamException(StreamErrorCode code) : base("NetStream error: " + code.ToString()) {
            ErrorCode = code;
        }
    }
}
