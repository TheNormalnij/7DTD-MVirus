using System;

namespace MVirus.Client.NetStreams
{
    public class NetStreamException : Exception
    {
        public NetStreamException(string message) : base(message) { }
    }
}
