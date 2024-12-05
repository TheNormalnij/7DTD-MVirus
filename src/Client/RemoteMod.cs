using System.IO;

namespace MVirus.Client
{
    enum RemoteModLoadState
    {
        None,
        Downloading,
        Starting,
        Running,
        Unloading,
        Failed
    }

    class RemoteMod
    {
        public readonly string name;
        private RemoteModLoadState State;

        public RemoteMod(string _name)
        {
            State = RemoteModLoadState.None;
            name = _name;
        }

        public void Load()
        {
            Log.Out("[MVirus] Load resource: " + name);
            State = RemoteModLoadState.Starting;

            State = RemoteModLoadState.Running;
        }

        public void Unload()
        {
            Log.Out("[MVirus] Unload resource: " + name);
            State = RemoteModLoadState.Unloading;
        }

        private string GetPath()
        {
            return Path.Combine(API.clientCachePath, name);
        }
    }
}
