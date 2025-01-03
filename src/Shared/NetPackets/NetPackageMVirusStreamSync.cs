using MVirus.Server;
using MVirus.Shared.NetStreams;
using System.Collections.Generic;

namespace MVirus.Shared.NetPackets
{
    public class NetPackageMVirusStreamSync : NetPackage
    {
        public override bool AllowedBeforeAuth => true;
        public override bool FlushQueue => true;

        private List<NetStreamSyncData> data;

        public NetPackageMVirusStreamSync Setup(List<NetStreamSyncData> data)
        {
            this.data = data;
            return this;
        }

        public override void read(PooledBinaryReader _reader)
        {
            var count = _reader.ReadByte();
            data = new List<NetStreamSyncData>();
            for (int i = 0; i < count; i++)
            {
                var item = new NetStreamSyncData {
                    streamId = _reader.ReadByte(),
                    readedCount = _reader.ReadInt64(),
                    bufferSize = _reader.ReadInt32(),
                };

                data.Add(item);
            }
        }

        public override void write(PooledBinaryWriter _writer)
        {
            base.write(_writer);
            _writer.Write((byte)data.Count);
            foreach (var item in data)
            {
                _writer.Write(item.streamId);
                _writer.Write(item.readedCount);
                _writer.Write(item.bufferSize);
            }
        }

        public override int GetLength()
        {
            return 1 + 13 * data.Count;
        }

        public override void ProcessPackage(World _world, GameManager _callbacks)
        {
            ServerModManager.netTransferManager?.HandleSyncData(Sender, data);
        }

    }
}
