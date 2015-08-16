using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace SampleOverwolfExtensionLibrary
{
    [Serializable()]
    public class Configuration
    {
        [XmlElement("ProjectDirectory")]
        public string ProjectDirectory { get; set; }

        [XmlElement("GameLogFilePath")]
        public string GameLogFilePath { get; set; }
    }
}
