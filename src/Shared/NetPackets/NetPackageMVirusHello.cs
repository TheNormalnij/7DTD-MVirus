using MVirus.Client;
using System;

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
        private string remoteHttpServer;

        public NetPackageMVirusHello Setup(RemoteFilesSource filesSource, ushort httpPort, string _remoteHttpServer)
        {
            remoteFilesSource = filesSource;
            localHttpPort = httpPort;
            remoteHttpServer = _remoteHttpServer;
            return this;
        }

        public override void read(PooledBinaryReader _reader)
        {
            MVirusProtocolVersion = _reader.ReadUInt32();
            MVirusMinimalBuild = _reader.ReadUInt32();
            remoteFilesSource = (RemoteFilesSource)_reader.ReadByte();

            switch (remoteFilesSource)
            {
                case RemoteFilesSource.LOCAL_HTTP:
                    localHttpPort = _reader.ReadUInt16();
                    break;
                case RemoteFilesSource.REMOTE_HTTP:
                    remoteHttpServer = _reader.ReadString();
                    break;
                case RemoteFilesSource.GAME_CONNECTION:
                    throw new NotImplementedException("GAME_CONNECTION is not implemented");
                default:
                    throw new Exception("Invalid remote file source");
            }
        }

        public override void write(PooledBinaryWriter _writer)
        {
            base.write(_writer);
            _writer.Write(MVirusProtocolVersion);
            _writer.Write(MVirusMinimalBuild);
            _writer.Write((byte)remoteFilesSource);

            switch (remoteFilesSource)
            {
                case RemoteFilesSource.LOCAL_HTTP:
                    _writer.Write(localHttpPort);
                    break;
                case RemoteFilesSource.REMOTE_HTTP:
                    _writer.Write(remoteHttpServer);
                    break;
                case RemoteFilesSource.GAME_CONNECTION:
                    throw new NotImplementedException("GAME_CONNECTION is not implemented");
                default:
                    throw new Exception("Invalid remote file source");
            }
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

            RemoteHttpInfo remoteHttp;

            if (remoteFilesSource == RemoteFilesSource.LOCAL_HTTP)
            {
                var ip = connectionManager.LastGameServerInfo.GetValue(GameInfoString.IP);
                remoteHttp = new RemoteHttpInfo(ip, localHttpPort);
            }
            else
            {
                remoteHttp = new RemoteHttpInfo(remoteHttpServer);
            }

            RemoteContentManager.RequestContent(remoteHttp);
        }

        public override int GetLength()
        {
            switch (remoteFilesSource)
            {
                case RemoteFilesSource.LOCAL_HTTP:
                    return 4 + 4 + 1 + 2;
                case RemoteFilesSource.REMOTE_HTTP:
                    return 4 + 4 + 1 + remoteHttpServer.Length;
                case RemoteFilesSource.GAME_CONNECTION:
                    return 4 + 4 + 1;
                default:
                    return 0;
            }
        }
    }
}
