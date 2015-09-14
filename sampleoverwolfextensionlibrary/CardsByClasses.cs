using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SampleOverwolfExtensionLibrary
{
    public class CardsByClasses
    {
       

        public class RootObject
        {
            public List<string> Druid { get; set; }
        }

        private static RootObject m_instanse=null;

        public static RootObject Instanse
        {
            get
            {
                if (m_instanse == null)
                {
                    string overwolfDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    string configFilePath = Path.Combine(overwolfDir, "Overwolf\\Ninja\\CardsClasses.json");
                    if (File.Exists(configFilePath))
                    {
                        string configuration = File.ReadAllText(configFilePath);
                        m_instanse = JsonConvert.DeserializeObject<RootObject>(configuration);
                    };
                }
                return m_instanse;
            }
            set { }
        }
    }

    
}
