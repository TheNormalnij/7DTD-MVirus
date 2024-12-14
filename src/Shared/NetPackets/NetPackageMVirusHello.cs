using MVirus.Client;

namespace MVirus.Shared.NetPackets
{
    public enum RemoteFilesSource
    {
        LOCAL_HTTP,
        REMOTE_HTTP,
        GAME_CONNECTION,
    }

    public class NetPackageMVirusHello : NetPackage
    {
        // Package config
        public override bool Compress => true;
        public override bool AllowedBeforeAuth => true;
        public override bool FlushQueue => true;
        public override NetPackageDirection PackageDirection => NetPackageDirection.ToClient;

        // Params
        private uint MVirusProtocolVersion = 0;
        private uint MVirusMinimalBuild = Version.MINIMAL_CLIENT_VERSION;
        private RemoteFilesSource remoteFilesSource;
        private ushort localHttpPort;

        public NetPackageMVirusHello Setup(RemoteFilesSource filesSource, ushort httpPort)
        {
            remoteFilesSource = filesSource;
            localHttpPort = httpPort;
            return this;
        }

        public override void read(PooledBinaryReader _reader)
        {
            MVirusProtocolVersion = _reader.ReadUInt32();
            MVirusMinimalBuild = _reader.ReadUInt32();
            remoteFilesSource = (RemoteFilesSource)_reader.ReadByte();
            if (remoteFilesSource == RemoteFilesSource.LOCAL_HTTP)
                localHttpPort = _reader.ReadUInt16();
        }

        public override void write(PooledBinaryWriter _writer)
        {
            base.write(_writer);
            _writer.Write(MVirusProtocolVersion);
            _writer.Write(MVirusMinimalBuild);
            _writer.Write((byte)remoteFilesSource);
            _writer.Write(localHttpPort);
        }

        // Client only
        public override void ProcessPackage(World _world, GameManager _callbacks)
        {
            var connectionManager = SingletonMonoBehaviour<ConnectionManager>.Instance;
            if (!connectionManager.IsConnected)
                return;

            if (MVirusProtocolVersion != 0)
            {
                Log.Warning("[MVirus] Protocol version missmatch");
                connectionManager.Disconnect();
            }

            if (MVirusMinimalBuild > Version.VERSION)
            {
                Log.Warning("[MVirus] Server requests MVirus " + MVirusMinimalBuild + ". Curent version: " + Version.VERSION);
                connectionManager.Disconnect();
            }

            var ip = connectionManager.LastGameServerInfo.GetValue(GameInfoString.IP);
            RemoteContentManager.RequestContent(new RemoteHttpInfo(ip, localHttpPort));
        }

        public override int GetLength()
        {
            return 4 + 4 + 1 + 2;
        }
    }
}
