using System;
using MVirus.Client;
using MVirus.Logger;

namespace MVirus.NetPackets
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
        public override bool AllowedBeforeAuth => true;
        public override bool FlushQueue => true;
        public override NetPackageDirection PackageDirection => NetPackageDirection.ToClient;

        // Params
        private System.Version serverVersion = Version.ModVersion;
        private System.Version minimalVersion = Version.MinimalClientVersion;
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
            serverVersion = System.Version.Parse(_reader.ReadString());
            minimalVersion = System.Version.Parse(_reader.ReadString());
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
                    break;
                default:
                    throw new Exception("Invalid remote file source");
            }
        }

        public override void write(PooledBinaryWriter _writer)
        {
            base.write(_writer);
            _writer.Write(serverVersion.ToString());
            _writer.Write(minimalVersion.ToString());
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
                    break;
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

            if (connectionManager.IsServer)
                return;

            if (minimalVersion > Version.ModVersion)
            {
                MVLog.Warning("Server requests MVirus " + minimalVersion + ". Current version: " + Version.ModVersion);
                connectionManager.Disconnect();
            }

            if (remoteFilesSource == RemoteFilesSource.GAME_CONNECTION)
            {
                RemoteContentManager.RequestContent();
                return;
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
