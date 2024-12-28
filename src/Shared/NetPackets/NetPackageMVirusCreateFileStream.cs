using MVirus.Client.NetStreams;
using MVirus.Server;

namespace MVirus.Shared.NetPackets
{
    public class NetPackageMVirusCreateFileStream : NetPackage
    {
        public override bool AllowedBeforeAuth => true;
        public override bool FlushQueue => true;

        private string filePath;
        private byte streamId;

        public NetPackageMVirusCreateFileStream Setup(string _filePath, byte id)
        {
            filePath = _filePath;
            streamId = id;
            return this;
        }

        public override void read(PooledBinaryReader _reader)
        {
            filePath = _reader.ReadString();
            streamId = _reader.ReadByte();
        }

        public override void write(PooledBinaryWriter _writer)
        {
            base.write(_writer);
            _writer.Write(filePath);
            _writer.Write(streamId);
        }

        public override int GetLength()
        {
            return filePath.Length + 1;
        }

        public override void ProcessPackage(World _world, GameManager _callbacks)
        {
            if (ServerModManager.netTransferManager != null)
                ServerModManager.netTransferManager.HandleStreamRequest(Sender, filePath, streamId);
            else
                Sender.SendPackage(NetPackageManager.GetPackage<NetPackageMVirusStreamError>().Setup(streamId, new NetStreamException(StreamErrorCode.NOT_SUPPORTED)));
        }
    }
}
