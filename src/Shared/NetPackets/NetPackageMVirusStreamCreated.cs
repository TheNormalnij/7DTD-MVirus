
namespace MVirus.Shared.NetPackets
{
    public class NetPackageMVirusStreamCreated : NetPackage
    {
        public override bool AllowedBeforeAuth => true;

        private byte streamId;
        private long streamSize;
        private bool compressedData;

        public NetPackageMVirusStreamCreated Setup(byte _streamId, long _streamSize, bool _compressedData)
        {
            compressedData = _compressedData;
            streamId = _streamId;
            streamSize = _streamSize;
            return this;
        }

        public override void read(PooledBinaryReader _reader)
        {
            streamId = _reader.ReadByte();
            streamSize = _reader.ReadInt64();
            compressedData = _reader.ReadBoolean();
        }

        public override void write(PooledBinaryWriter _writer)
        {
            base.write(_writer);
            _writer.Write(streamId);
            _writer.Write(streamSize);
            _writer.Write(compressedData);
        }

        public override int GetLength()
        {
            return 10;
        }

        public override void ProcessPackage(World _world, GameManager _callbacks)
        {
            API.incomingStreamHandler.HandleStreamOpen(streamId, streamSize, compressedData);
        }
    }
}
