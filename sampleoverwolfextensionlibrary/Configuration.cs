using log4net;
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
        private static string m_configFilePath = string.Empty;
        private static readonly ILog m_logger = LogManager.GetLogger(typeof(Configuration));

        public static RootObject Instance
        {
            get
            {
                if (m_instance == null)
                {
                    lock (SYNC_OBJ)
                    {
                        string overwolfDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                        m_configFilePath = Path.Combine(overwolfDir, "Overwolf\\Ninja\\NinjaConfig.json");
                        ReloadConfiguration();
                    }
                }
                return m_instance;
            }
        }

        public static void LoadConfigFromFile(string configFilePath)
        {
            if (File.Exists(m_configFilePath))
            {
                string configuration = File.ReadAllText(m_configFilePath);
                m_instance = JsonConvert.DeserializeObject<RootObject>(configuration);
            };
            m_instance.JSONCardsFilePath = Environment.ExpandEnvironmentVariables(m_instance.JSONCardsFilePath);
            m_instance.JSONCardsByClasses = Environment.ExpandEnvironmentVariables(m_instance.JSONCardsByClasses);
            m_instance.AppLogConfigFilePath = Environment.ExpandEnvironmentVariables(m_instance.AppLogConfigFilePath);
            m_instance.AppLogFilePath = Environment.ExpandEnvironmentVariables(m_instance.AppLogFilePath);
            m_instance.GameLogFilePath = Environment.ExpandEnvironmentVariables(m_instance.GameLogFilePath);
            m_instance.TempFolder = Environment.ExpandEnvironmentVariables(m_instance.TempFolder);
            m_instance.OCR.TesseractDataPath = Environment.ExpandEnvironmentVariables(m_instance.OCR.TesseractDataPath);
        }

        public static void ReloadConfiguration()
        {
            LoadConfigFromFile(m_configFilePath);
        }

        public static void SaveConfiguration(string configFilePath)
        {
            if (m_instance != null)
            {
                string output = JsonConvert.SerializeObject(m_instance);
                try
                {
                    File.WriteAllText(configFilePath, output);
                }
                catch (Exception e)
                {
                    m_logger.Error(string.Format("Can't write configuration file to {0}, exception: {1}", configFilePath, e.Message));
                }
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
            public string JSONCardsByClasses { get; set; }
            public string AppLogConfigFilePath { get; set; }
            public string AppLogFilePath { get; set; }
            public string TempFolder { get; set; }
            public OCR OCR { get; set; }
            public JS JS { get; set; }
        }
    }
}
