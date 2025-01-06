
namespace MVirus.Shared.NetPackets
{
    public class NetPackageMVirusStreamClosed : NetPackage
    {
        public override bool AllowedBeforeAuth => true;

        private byte streamId;

        public NetPackageMVirusStreamClosed Setup(byte _streamId)
        {
            streamId = _streamId;
            return this;
        }

        public override void read(PooledBinaryReader _reader)
        {
            streamId = _reader.ReadByte();
        }

        public override void write(PooledBinaryWriter _writer)
        {
            base.write(_writer);
            _writer.Write(streamId);
        }

        public override int GetLength()
        {
            return 1;
        }

        public override void ProcessPackage(World _world, GameManager _callbacks)
        {
            API.incomingStreamHandler.HandleStreamClose(streamId);
        }
    }
}
