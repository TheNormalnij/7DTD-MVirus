
namespace MVirus.Shared.NetPackets
{
    enum UpdateType
    {
        CLOSE,
        CREATE
    }

    public class NetPackageMVirusStreamUpdate : NetPackage
    {
        public override bool AllowedBeforeAuth => true;
        public override bool FlushQueue => true;

        private byte streamId;
        private UpdateType code;
        private long streamSize;

        public NetPackageMVirusStreamUpdate SetupClose(byte _streamId)
        {
            code = UpdateType.CLOSE;
            streamId = _streamId;
            return this;
        }

        public NetPackageMVirusStreamUpdate SetupCreate(byte _streamId, long _streamSize)
        {
            code = UpdateType.CREATE;
            streamId = _streamId;
            streamSize = _streamSize;
            return this;
        }

        public override void read(PooledBinaryReader _reader)
        {
            streamId = _reader.ReadByte();
            code = (UpdateType)_reader.ReadByte();
            switch (code)
            {
                case UpdateType.CREATE:
                    streamSize = _reader.ReadInt64();
                    break;

                default:
                    break;
            }
        }

        public override void write(PooledBinaryWriter _writer)
        {
            base.write(_writer);
            _writer.Write(streamId);
            _writer.Write((byte)code);
            switch (code)
            {
                case UpdateType.CREATE:
                    _writer.Write(streamSize);
                    break;

                default:
                    break;
            }
        }

        public override int GetLength()
        {
            if (code == UpdateType.CREATE)
                return 10;

            return 2;
        }

        public override void ProcessPackage(World _world, GameManager _callbacks)
        {
           switch (code) {
                case UpdateType.CREATE:
                {
                    API.incomingStreamHandler.HandleStreamOpen(streamId, streamSize);
                    break;
                }
                case UpdateType.CLOSE:
                {
                    API.incomingStreamHandler.HandleStreamClose(streamId);
                    break;
                }
                default:
                    break;
            }
        }
    }
}
