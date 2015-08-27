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
                        m_instance.TempFolder = Environment.ExpandEnvironmentVariables(m_instance.TempFolder);
                        m_instance.OCR.TesseractDataPath = Environment.ExpandEnvironmentVariables(m_instance.OCR.TesseractDataPath);
                    }
                }
                return m_instance;
            }
        }
        public class SplitToStrips
        {
            public double deckNameRatio { get; set; }
            public double stripRatio { get; set; }
            public double bottomMenuRatio { get; set; }
        }

        public class IsNotWhiteRange
        {
            public int minVal { get; set; }
            public int maxDiff { get; set; }
            public int minAlpha { get; set; }
        }

        public class IsNotInYellowRange
        {
            public int delta { get; set; }
        }

        public class CropWidth
        {
            public double rightSideRatio { get; set; }
            public double leftSideRatio { get; set; }
        }

        public class RecognizeStripNumber
        {
            public double numberToStripWidthRatio { get; set; }
        }

        public class OCR
        {
            public string TesseractDataPath { get; set; }
            public SplitToStrips SplitToStrips { get; set; }
            public IsNotWhiteRange isNotWhiteRange { get; set; }
            public IsNotInYellowRange isNotInYellowRange { get; set; }
            public CropWidth CropWidth { get; set; }
            public RecognizeStripNumber recognizeStripNumber { get; set; }
        }

        public class JS
        {
            public string ScreenShotFolder { get; set; }
        }

        public class RootObject
        {
            public string GameLogFilePath { get; set; }
            public string JSONCardsFilePath { get; set; }
            public string AppLogConfigFilePath { get; set; }
            public string AppLogFilePath { get; set; }
            public string TempFolder { get; set; }
            public OCR OCR { get; set; }
            public JS JS { get; set; }
        }
    }
}
