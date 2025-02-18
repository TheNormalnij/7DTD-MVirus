﻿using MVirus.Logger;
using MVirus.NetStreams;
using MVirus.Server;

namespace MVirus.NetPackets
{
    enum RemoteStreamType
    {
        File = 0,
    }

    public class NetPackageMVirusStreamCreate : NetPackage
    {
        public override bool AllowedBeforeAuth => true;

        private string filePath;
        private byte streamId;
        private RemoteStreamType streamType;

        public NetPackageMVirusStreamCreate Setup(string _filePath, byte id)
        {
            filePath = _filePath;
            streamId = id;
            streamType = RemoteStreamType.File;
            return this;
        }

        public override void read(PooledBinaryReader _reader)
        {
            filePath = _reader.ReadString();
            streamId = _reader.ReadByte();
            streamType = (RemoteStreamType)_reader.ReadByte();
        }

        public override void write(PooledBinaryWriter _writer)
        {
            base.write(_writer);
            _writer.Write(filePath);
            _writer.Write(streamId);
            _writer.Write((byte)streamType);

            MVLog.Debug("Write create request");
        }

        public override int GetLength()
        {
            return filePath.Length + 1 + 1;
        }

        public override void ProcessPackage(World _world, GameManager _callbacks)
        {
            if (ServerModManager.netTransferManager != null || streamType != RemoteStreamType.File)
                ServerModManager.netTransferManager.HandleStreamRequest(Sender, filePath, streamId);
            else
                Sender.SendPackage(NetPackageManager.GetPackage<NetPackageMVirusStreamError>().Setup(streamId, new NetStreamException(StreamErrorCode.NOT_SUPPORTED)));
        }
    }
}
