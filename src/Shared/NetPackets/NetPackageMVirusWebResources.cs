using MVirus.Client;
using System.Collections.Generic;

namespace MVirus.Shared.NetPackets
{
    public class NetPackageMVirusWebResources : NetPackage
    {
        private List<ServerFileInfo> files;

        public override bool Compress => true;
        public override bool AllowedBeforeAuth => true;
        public override bool FlushQueue => true;
        public override NetPackageDirection PackageDirection => NetPackageDirection.ToClient;

        public NetPackageMVirusWebResources Setup(List<ServerFileInfo> list)
        {
            files = list;
            return this;
        }

        public override void read(PooledBinaryReader _reader)
        {
            files = new List<ServerFileInfo>();

            var count = _reader.ReadInt32();

            for (int i = 0; i < count; i++)
            {
                var name = _reader.ReadString();
                var size = _reader.ReadInt64();
                var crc = _reader.ReadInt32();

                files.Add(new ServerFileInfo(name, size, crc));
            }
        }

        public override void write(PooledBinaryWriter _writer)
        {
            base.write(_writer);
            _writer.Write(files.Count);

            foreach (var item in files)
            {
                _writer.Write(item.Path);
                _writer.Write(item.Size);
                _writer.Write(item.Crc);
            }
        }

        public override void ProcessPackage(World _world, GameManager _callbacks)
        {
            RemoteContentManager.RequestServerMods(files);
        }

        public override int GetLength()
        {
            var length = 4;
            foreach (var item in files)
            {
                length += item.Path.Length + 12;
            }
            return length;
        }
    }
}
