
namespace MVirus.NetPackets
{
    public class NetPackageMVirusStreamClosed : NetPackage
    {
        public override bool AllowedBeforeAuth => true;

        private byte streamId;
        private bool finished;

        public NetPackageMVirusStreamClosed Setup(byte _streamId, bool _finished)
        {
            streamId = _streamId;
            finished = _finished;
            return this;
        }

        public override void read(PooledBinaryReader _reader)
        {
            streamId = _reader.ReadByte();
            finished = _reader.ReadBoolean();
        }

        public override void write(PooledBinaryWriter _writer)
        {
            base.write(_writer);
            _writer.Write(streamId);
            _writer.Write(finished);
        }

        public override int GetLength()
        {
            return 2;
        }

        public override void ProcessPackage(World _world, GameManager _callbacks)
        {
            API.incomingStreamHandler.HandleStreamClose(streamId, finished);
        }
    }
}
