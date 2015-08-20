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
        // TODO: Add application Temp folder to configuration file.
        public static RootObject Instance
        {
            get
            {
                if (m_instance == null)
                {
                    lock (SYNC_OBJ)
                    {
                        string overwolfDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                        string configFilePath = Path.Combine(overwolfDir, "Overwolf\\Ninja\\NinjaConfig.json");
                        if (File.Exists(configFilePath))
                        {
                            string configuration = File.ReadAllText(configFilePath);
                            m_instance = JsonConvert.DeserializeObject<RootObject>(configuration);
                        };
                        m_instance.JSONCardsFilePath = Environment.ExpandEnvironmentVariables(m_instance.JSONCardsFilePath);
                        m_instance.AppLogConfigFilePath = Environment.ExpandEnvironmentVariables(m_instance.AppLogConfigFilePath);
                        m_instance.AppLogFilePath = Environment.ExpandEnvironmentVariables(m_instance.AppLogFilePath);
                        m_instance.GameLogFilePath = Environment.ExpandEnvironmentVariables(m_instance.GameLogFilePath);
                        m_instance.OCR.TesseractDataPath = Environment.ExpandEnvironmentVariables(m_instance.OCR.TesseractDataPath);
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
            public double RightSideCropRatio { get; set; }
            public double LeftSideCropRatio { get; set; }
        }

        public class RootObject
        {
            public string GameLogFilePath { get; set; }
            public string JSONCardsFilePath { get; set; }
            public string AppLogConfigFilePath { get; set; }
            public string AppLogFilePath { get; set; }
            public OCR OCR { get; set; }
        }
    }
}
