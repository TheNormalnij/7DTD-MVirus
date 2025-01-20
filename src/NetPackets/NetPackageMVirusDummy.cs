using MVirus.Logger;

namespace MVirus.NetPackets
{
    public class NetPackageMVirusDummy  : NetPackage
    {
        // Package config
        public override bool AllowedBeforeAuth => true;


        public override void read(PooledBinaryReader _reader)
        {
            MVLog.Error("Protocol reads dummy package. This should never happen.");
        }

        public override void write(PooledBinaryWriter _writer)
        {
            base.write(_writer);
            MVLog.Error("Protocol writes dummy package. This should never happen.");
        }

        public override void ProcessPackage(World _world, GameManager _callbacks)
        {
            MVLog.Error("Something uses NetPackageMVirusDummy");
        }

        public override int GetLength()
        {
            return 0;
        }
    }
}
