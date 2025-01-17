using System;
using System.Collections.Generic;
using MVirus.Config;
using MVirus.Logger;
using MVirus.ModInfo;

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

    internal struct LoadedAtlasInfo
    {
        public UIAtlas atlas;
        public ModManager.AtlasManagerEntry ame;
    }

    class RemoteMod
    {
        public readonly string name;
        public readonly string dirName;
        private readonly ServerFileInfo[] files;

        private readonly List<LoadedAtlasInfo> atlases;
        private readonly List<string> atlasManagers;

        public string Path => MVirusConfig.ClientCachePath + "/" + dirName;

        public RemoteModLoadState State { get; private set; }

        public RemoteMod(ServerModInfo remoteMod)
        {
            State = RemoteModLoadState.Stopped;
            name = remoteMod.Name;
            dirName = remoteMod.DirName;
            files = remoteMod.Files;
            atlases = new List<LoadedAtlasInfo>();
            atlasManagers = new List<string>();
        }

        public void Load()
        {
            MVLog.Out("Load server mode: " + name);
            State = RemoteModLoadState.Starting;

            try
            {
                LoadAtlases();
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
            MVLog.Out("Unload server mod: " + name);
            State = RemoteModLoadState.Unloading;
            try
            {
                UnloadAtlases();
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
                State = RemoteModLoadState.Failed;
                return;
            }
            State = RemoteModLoadState.Stopped;
        }

        private void LoadAtlases()
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
                    MVLog.Out("Creating new atlas '" + dirName + "' for mod '" + name + "'");
                    ModManager.RegisterAtlasManager(MultiSourceAtlasManager.Create(ModManager.atlasesParentGo, dirName), _createdByMod: true, ModManager.defaultShader);
                    ame = ModManager.atlasManagers[dirName];

                    atlasManagers.Add(dirName);
                }

                var enumerator = UIAtlasFromFolder.CreateUiAtlasFromFolder(Path + "/UIAtlases/" + dirName, ame.Shader, (UIAtlas _atlas) =>
                {
                    _atlas.transform.parent = ame.Manager.transform;
                    ame.Manager.AddAtlas(_atlas, true);
                    ame.OnNewAtlasLoaded?.Invoke(_atlas, true);

                    this.atlases.Add(new LoadedAtlasInfo { ame = ame, atlas = _atlas });
                });

                while (enumerator.MoveNext()) { }
            }
        }

        private void UnloadAtlases()
        {
            foreach (var item in atlases)
            {
                var toRemove = item.ame.Manager.atlases.Find(baseAtlass => baseAtlass.Atlas == item.atlas);
                item.ame.Manager.atlases.Remove(toRemove);
            }

            foreach (var item in atlases)
                item.ame.Manager.recalcSpriteSources();

            atlases.Clear();

            foreach (var item in atlasManagers)
                ModManager.atlasManagers.Remove(item);

            atlasManagers.Clear();
        }
    }
}
