
using MVirus.Server;

namespace MVirus.Shared.NetPackets
{
    enum UpdateType
    {
        CLOSE,
    }

    public class NetPackageMVirusStreamUpdate : NetPackage
    {
        public override bool AllowedBeforeAuth => true;
        public override bool FlushQueue => true;

        private byte streamId;
        private UpdateType code;

        public NetPackageMVirusStreamUpdate SetupClose(byte _streamId)
        {
            code = UpdateType.CLOSE;
            streamId = _streamId;
            return this;
        }

        public override void read(PooledBinaryReader _reader)
        {
            streamId = _reader.ReadByte();
            code = (UpdateType)_reader.ReadByte();
        }

        public override void write(PooledBinaryWriter _writer)
        {
            base.write(_writer);
            _writer.Write(streamId);
            _writer.Write((byte)code);
        }

        public override int GetLength()
        {
            return 2;
        }

        public override void ProcessPackage(World _world, GameManager _callbacks)
        {
            ServerModManager.netTransferManager?.HandleStreamError(Sender, streamId, fileStreamErrorCode);
        }
    }
}
