using MVirus.Client;
using System.Collections.Generic;
using System.Linq;

namespace MVirus.Shared.NetPackets
{
    public class NetPackageMVirusWebResources : NetPackage
    {
        private ServerModInfo[] serverMods;

        public override bool Compress => true;
        public override bool AllowedBeforeAuth => true;
        public override bool FlushQueue => true;
        public override NetPackageDirection PackageDirection => NetPackageDirection.ToClient;

        public NetPackageMVirusWebResources Setup(List<ServerModInfo> list)
        {
            // Do not send mods without files
            serverMods = Enumerable.Where(list, mod => mod.Files.Length != 0)
                                   .ToArray();

            return this;
        }

        public override void read(PooledBinaryReader _reader)
        {
            var count = _reader.ReadUInt16();
            serverMods = new ServerModInfo[count];

            for (int i = 0; i < count; i++)
            {
                var name = _reader.ReadString();
                var dirName = _reader.ReadString();
                var filesCount = _reader.ReadUInt16();

                var files = new ServerFileInfo[filesCount];
                serverMods[i] = new ServerModInfo { Name = name, DirName = dirName, Files = files };

                for (int j = 0; j < filesCount; j++)
                    files[j] = new ServerFileInfo(_reader.ReadString(), _reader.ReadInt64(), _reader.ReadInt32());
            }
        }

        public override void write(PooledBinaryWriter _writer)
        {
            base.write(_writer);
            _writer.Write((ushort)serverMods.Length);

            foreach (var item in serverMods)
            {
                _writer.Write(item.Name);
                _writer.Write(item.DirName);
                _writer.Write((ushort)item.Files.Length);
                foreach (var file in item.Files)
                {
                    _writer.Write(file.Path);
                    _writer.Write(file.Size);
                    _writer.Write(file.Crc);
                }
            }
        }

        public override void ProcessPackage(World _world, GameManager _callbacks)
        {
            RemoteContentManager.RequestServerMods(serverMods);
        }

        public override int GetLength()
        {
            var length = 2;
            foreach (var item in serverMods)
            {
                length += item.Name.Length + item.DirName.Length + 2;
                foreach (var fileInfo in item.Files)
                {
                    length += fileInfo.Path.Length + 12;
                }
            }
            return length;
        }
    }
}
