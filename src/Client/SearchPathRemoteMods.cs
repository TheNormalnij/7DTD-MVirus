using System.Collections.Generic;
using System.Text.RegularExpressions;
using static PathAbstractions;

namespace MVirus.Client
{
    public class SearchPathRemoteMods : SearchPath
    {
        public override bool CanMatch => RemoteContentManager.remoteMods.Count != 0;

        public SearchPathRemoteMods(string _relativePath, bool _pathIsTarget = false)
            : base(_relativePath, _pathIsTarget) { }

        public override AbstractedLocation GetLocation(string _name, string _worldName, string _gameName)
        {
            if (UseCache(_worldName, _gameName))
                return GetCachedLocation(_name);

            foreach (RemoteMod mod in RemoteContentManager.remoteMods.Values)
            {
                AbstractedLocation location = getLocationSingleBase(EAbstractedLocationType.Mods, mod.Path + "/" + RelativePath, _name, _worldName, _gameName, API.instance);
                if (!location.Equals(AbstractedLocation.None))
                    return location;
            }

            return AbstractedLocation.None;
        }

        public override void GetAvailablePathsList(List<AbstractedLocation> _targetList, Regex _nameMatch, string _worldName, string _gameName, bool _ignoreDuplicateNames)
        {
            if (UseCache(_worldName, _gameName))
            {
                GetCachedPathList(_targetList, _nameMatch, _ignoreDuplicateNames);
                return;
            }

            foreach (RemoteMod mod in RemoteContentManager.remoteMods.Values)
                getAvailablePathsSingleBase(_targetList, EAbstractedLocationType.Mods, mod.Path + "/" + RelativePath, _nameMatch, _worldName, _gameName, _ignoreDuplicateNames, API.instance);
        }

        public override void PopulateCache()
        {
            List<AbstractedLocation> list = new List<AbstractedLocation>();
            foreach (RemoteMod mod in RemoteContentManager.remoteMods.Values)
                getAvailablePathsSingleBase(list, EAbstractedLocationType.Mods, mod.Path + "/" + RelativePath, null, null, null, _ignoreDuplicateNames: false, API.instance);

            locationsCache.Clear();
            for (int i = 0; i < list.Count; i++)
            {
                AbstractedLocation item = list[i];
                if (!locationsCache.TryGetValue(item.Name, out var value))
                {
                    value = new List<AbstractedLocation>();
                    locationsCache[item.Name] = value;
                }

                value.Add(item);
            }

            locationsCachePopulated = true;
        }
    }
}
