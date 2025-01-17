using System;
using System.Xml.Linq;
using MVirus.NetPackets;

namespace MVirus.Config
{
    public class MVirusConfigXml
    {
        public static void Load(XmlFile _xmlFile)
        {
            XElement root = _xmlFile.XmlDoc.Root;
            if (!root.HasElements)
                throw new Exception("No root element found for MVirus config!");

            foreach (XElement item in root.Elements("property"))
            {
                switch (item.GetAttribute("name"))
                {
                    case "HttpPort":
                        {
                            MVirusConfig.FilesHttpPort = ushort.Parse(item.GetAttribute("value"));
                            break;
                        }

                    case "FileTransferType":
                        {
                            var value = ushort.Parse(item.GetAttribute("value"));

                            if (value > ((ushort)RemoteFilesSource.GAME_CONNECTION))
                                throw new Exception("Invalid FileTransferType config");

                            MVirusConfig.RemoteFilesSource = (RemoteFilesSource)value;
                            break;
                        }

                    case "ExternalHTTPServerAddr":
                        {
                            MVirusConfig.RemoteHttpAddr = item.GetAttribute("value");
                            break;
                        }
                    case "StaticCompression":
                        {
                            MVirusConfig.StaticFileCompression = item.GetAttribute("value") == "true";
                            break;
                        }
                    case "ActiveCompression":
                        {
                            MVirusConfig.ActiveFileCompression = item.GetAttribute("value") == "true";
                            break;
                        }
                    case "CacheAllRemoteFiles":
                        {
                            MVirusConfig.CacheAllRemoteFiles = item.GetAttribute("value") == "true";
                            break;
                        }
                    case "ShareMods":
                        {
                            MVirusConfig.IsModSharingEnabled = item.GetAttribute("value") == "true";
                            break;
                        }
                }
            }

            foreach (XElement item in root.Elements("mod"))
            {
                var name = item.GetAttribute("name");
                var share = item.GetAttribute("share") == "true";

                if (!string.IsNullOrEmpty(name) && !share)
                    MVirusConfig.IgnoredMods.Add(name);
            }
        }
    }
}
