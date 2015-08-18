using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace SampleOverwolfExtensionLibrary
{
    public class Configuration
    {
        private static RootObject m_instance = null;
        private static object SYNC_OBJ = new object();

        public static RootObject Instance
        {
            get
            {
                if (m_instance == null)
                {
                    lock (SYNC_OBJ)
                    {
                        string overwolfDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                        string configFilePath = Path.Combine(overwolfDir, "Ninja\\NinjaConfig.json");
                        if (File.Exists(configFilePath))
                        {
                            string configuration = File.ReadAllText(configFilePath);
                            m_instance = JsonConvert.DeserializeObject<RootObject>(configuration);
                        };
                    }
                }
                return m_instance;
            }
        }

        public class OCR
        {
            public string TesseractDataPath { get; set; }
            public int IsWhiteRangeMinVal { get; set; }
            public int IsWhiteRangeMaxDiff { get; set; }
            public int IsWhiteRangeMinAlpha { get; set; }
        }

        public class RootObject
        {
            public string ProjectDirectory { get; set; }
            public string GameLogFilePath { get; set; }
            public string JSONCardsFilePath { get; set; }
            public OCR OCR { get; set; }
        }
    }
}
