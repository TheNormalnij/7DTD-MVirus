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
                    case "MV_HttpPath":
                        {
                            MVirusConfig.FilesHttpPort = ushort.Parse(item.GetAttribute("value"));
                            break;
                        }
                }
            }
        }
    }
}
