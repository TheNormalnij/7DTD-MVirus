using MVirus.Server;

namespace MVirus.NetPackets
{
    public class NetPackageMVirusHelloResponse : NetPackage
    {
        public override bool Compress => false;
        public override bool AllowedBeforeAuth => true;
        public override NetPackageDirection PackageDirection => NetPackageDirection.ToServer;

        public NetPackageMVirusHelloResponse Setup()
        {
            return this;
        }

        public override void read(PooledBinaryReader _reader)
        {
        }

        public override void write(PooledBinaryWriter _writer)
        {
            base.write(_writer);
        }

        public override void ProcessPackage(World _world, GameManager _callbacks)
        {
            Sender.SendPackage(NetPackageManager.GetPackage<NetPackageMVirusWebResources>().Setup(ContentScanner.loadedMods));
        }

        public override int GetLength()
        {
            return 0;
        }
    }
}
