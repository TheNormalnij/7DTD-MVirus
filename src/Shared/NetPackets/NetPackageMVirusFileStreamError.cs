using MVirus.Server;

namespace MVirus.Shared.NetPackets
{
    public enum FileStreamErrorCode
    {
        NOT_FOUND,
        NOT_SUPPORTED,
        UNKNOWN_ERROR,
    }

    public class NetPackageMVirusFileStreamError : NetPackage {
        private byte streamId;
        private FileStreamErrorCode fileStreamErrorCode;

        public NetPackageMVirusFileStreamError Setup(byte _streamId, FileStreamErrorCode code)
        {
            fileStreamErrorCode = code;
            streamId = _streamId;
            return this;
        }

        public override void read(PooledBinaryReader _reader)
        {
            streamId = _reader.ReadByte();
            fileStreamErrorCode = (FileStreamErrorCode)_reader.ReadByte();
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
