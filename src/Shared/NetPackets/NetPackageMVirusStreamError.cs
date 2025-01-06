using MVirus.Shared.NetStreams;
using MVirus.Server;

namespace MVirus.Shared.NetPackets
{
    public class NetPackageMVirusStreamError : NetPackage {
        public override bool AllowedBeforeAuth => true;

        private byte streamId;
        private StreamErrorCode fileStreamErrorCode;

        public NetPackageMVirusStreamError Setup(byte _streamId, NetStreamException ex)
        {
            fileStreamErrorCode = ex.ErrorCode;
            streamId = _streamId;
            return this;
        }

        public override void read(PooledBinaryReader _reader)
        {
            streamId = _reader.ReadByte();
            fileStreamErrorCode = (StreamErrorCode)_reader.ReadByte();
        }

        public override void write(PooledBinaryWriter _writer)
        {
            base.write(_writer);
            _writer.Write(streamId);
            _writer.Write((byte)fileStreamErrorCode);
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
