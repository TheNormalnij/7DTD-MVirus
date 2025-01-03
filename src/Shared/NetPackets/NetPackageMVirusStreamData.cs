namespace MVirus.Shared.NetPackets
{
    public class NetPackageMVirusStreamData : NetPackage
    {
        public override bool AllowedBeforeAuth => true;
        public override bool FlushQueue => true;

        public bool WasWritted { get; private set; } = false;

        private byte streamId;
        private byte[] data;
        private int count;

        public NetPackageMVirusStreamData Setup(byte streamId, byte[] data, int count)
        {
            this.data = data;
            this.streamId = streamId;
            this.count = count;
            return this;
        }

        public override void read(PooledBinaryReader _reader)
        {
            streamId = _reader.ReadByte();
            count = _reader.ReadInt32();
            data = _reader.ReadBytes(count);
        }

        public override void write(PooledBinaryWriter _writer)
        {
            base.write(_writer);
            _writer.Write(streamId);
            _writer.Write(count);
            _writer.Write(data, 0, count);

            WasWritted = true;
        }

        public override int GetLength()
        {
            return 1 + 4 + count;
        }

        public override void ProcessPackage(World _world, GameManager _callbacks)
        {
            API.incomingStreamHandler.HandleIncomingData(streamId, data);
        }

    }
}
