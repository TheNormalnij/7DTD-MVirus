using MVirus.Shared;
using System;
using System.IO;

namespace MVirus.Client
{
    enum RemoteModLoadState
    {
        Stopped,
        Downloading,
        Starting,
        Running,
        Unloading,
        Failed
    }

    class RemoteMod
    {
        public readonly string name;
        private readonly ServerFileInfo[] files;

        private RemoteModLoadState State;

        public RemoteMod(ServerModInfo remoteMod)
        {
            State = RemoteModLoadState.Stopped;
            name = remoteMod.Name;
            files = remoteMod.Files;
        }

        public void Load()
        {
            Log.Out("[MVirus] Load resource: " + name);
            State = RemoteModLoadState.Starting;

            try
            {
                LoadAtlasses();
            } catch (Exception ex)
            {
                Log.Exception(ex);
                State = RemoteModLoadState.Failed;
                return;
            }

            State = RemoteModLoadState.Running;
        }

        public void Unload()
        {
            Log.Out("[MVirus] Unload resource: " + name);
            State = RemoteModLoadState.Unloading;
        }

        private void LoadAtlasses()
        {
            var atlases = new HashSetList<string>();

            foreach (var file in files)
            {
                if (file.Path.StartsWith("UIAtlases/"))
                {
                    var endIndex = file.Path.IndexOf('/', 10);
                    if (endIndex > 0)
                    {
                        var dirname = file.Path.Substring(10, endIndex - 10);
                        atlases.Add(dirname);
                    }
                }
            }

            foreach (var dirName in atlases.list)
            {
                if (!ModManager.atlasManagers.TryGetValue(dirName, out var ame))
                {
                    Log.Out("[MVirus] Creating new atlas '" + dirName + "' for mod '" + name + "'");
                    ModManager.RegisterAtlasManager(MultiSourceAtlasManager.Create(ModManager.atlasesParentGo, dirName), _createdByMod: true, ModManager.defaultShader);
                    ame = ModManager.atlasManagers[dirName];
                }

                var enumerator = UIAtlasFromFolder.CreateUiAtlasFromFolder(Path.Combine(API.clientCachePath, name, "UIAtlases", dirName), ame.Shader, (UIAtlas _atlas) =>
                {
                    _atlas.transform.parent = ame.Manager.transform;
                    ame.Manager.AddAtlas(_atlas, true);
                    ame.OnNewAtlasLoaded?.Invoke(_atlas, true);
                });

                while (enumerator.MoveNext()) { }
            }
        }
    }
}
