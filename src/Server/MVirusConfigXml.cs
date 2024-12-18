using MVirus.Shared.NetPackets;
using System;
using System.Xml.Linq;

namespace MVirus.Server
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

                            if (value > ((ushort)RemoteFilesSource.REMOTE_HTTP))
                                throw new Exception("Invalid FileTransferType config");

                            MVirusConfig.RemoteFilesSource = (RemoteFilesSource)value;
                            break;
                        }

                    case "ExternalHTTPServerAddr":
                        {
                            MVirusConfig.RemoteHttpAddr = item.GetAttribute("value");
                            break;
                        }
                }
            }
        }
    }
}
