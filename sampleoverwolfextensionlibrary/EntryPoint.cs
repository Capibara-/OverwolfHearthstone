using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using System.Xml.Serialization;
using Newtonsoft.Json;
using System.Configuration;
using System.Reflection;

namespace SampleOverwolfExtensionLibrary
{
    // JS -> C# : Create a class method that will be called by the JS, if a return value is needed a callback delegate will be used.
    // C# -> JS : Create an Action<object> event that the JS can register a listener for, whenever a C# to JS call is required simply fire the event
    // and the JS listener will be called.
    public class SampleOverwolfExtension : IDisposable
    {
        private static int m_delay = 900;
        private static long m_lastOffset = 0;
        public static int m_index = 0;
        private Configuration m_config;
        private Dictionary<string, Card> m_AllCards = null;
        public static List<Card> m_MyDeck = new List<Card>();
        public const int DECK_SIZE = 30;
        Regex cardMovementRegex = new Regex(@"\w*(name=(?<name>(.+?(?=id)))).*(cardId=(?<Id>(\w*))).*(zone\ from\ (?<from>((\w*)\s*)*))((\ )*->\ (?<to>(\w*\s*)*))*.*");

        public SampleOverwolfExtension(int nativeWindowHandle)
        {
        }

        public SampleOverwolfExtension()
        {
        }


        public void init(Action<object> callback)
        {
            // Load configuration from file:
            string overwolfDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string configFilePath = Path.Combine(overwolfDir, "Ninja\\NinjaConfig.xml");
            XmlSerializer ser = new XmlSerializer(typeof(Configuration));
            XmlDocument doc = new XmlDocument();
            doc.Load(configFilePath);
            XmlElement root = doc.DocumentElement;
            using (StringReader sr = new StringReader(root.OuterXml))
            {
                try
                {
                    m_config = (Configuration)ser.Deserialize(sr);
                }
                catch (Exception e)
                {
                    // TODO: Hanlde bad xml.
                }
            }

            if (m_AllCards == null)
            {
                Card c = null;
                string path = Path.Combine(m_config.ProjectDirectory, @"Files\XML\enGB.xml");
                ser = new XmlSerializer(typeof(Card));
                m_AllCards = new Dictionary<string, Card>();
                doc = new XmlDocument();
                doc.Load(path);
                root = doc.DocumentElement;
                if (root != null)
                {
                    XmlNodeList nodes = root.SelectNodes("Card");

                    if (nodes != null)
                        foreach (XmlNode node in nodes)
                        {
                            using (StringReader sr = new StringReader(node.OuterXml))
                            {
                                try
                                {
                                    c = (Card)ser.Deserialize(sr);
                                    c.Played = "false";
                                    m_AllCards.Add(c.CardId, c);


                                }
                                catch (Exception e)
                                {
                                    // TODO: Handle bad XML file.
                                }
                            }
                        }
                }
            }

            BackgroundWorker bw = new BackgroundWorker();
            callback(string.Format("Initialized cards, count: {0}", m_AllCards.Count));
            bw.DoWork += new DoWorkEventHandler(
                delegate (object o, DoWorkEventArgs args)
                {
                    while (true)
                    {

                        using (
                            FileStream fs = new FileStream(m_config.GameLogFilePath, FileMode.Open, FileAccess.Read,
                                FileShare.ReadWrite))
                        {

                            fs.Seek(m_lastOffset, SeekOrigin.Begin);
                            if (fs.Length != m_lastOffset)
                            {
                                using (StreamReader sr = new StreamReader(fs))
                                {
                                    string newLine = sr.ReadToEnd();
                                    m_lastOffset = fs.Length;
                                    if (cardMovementRegex.IsMatch(newLine))
                                    {
                                        //TODO add if (card is from -to our direction
                                        Match match = cardMovementRegex.Match(newLine);
                                        string id = match.Groups["Id"].Value.Trim();
                                        string name = match.Groups["name"].Value.Trim();
                                        string from = match.Groups["from"].Value.Trim();
                                        string to = match.Groups["to"].Value.Trim();
                                        if (id != "" && to.Contains("FRIENDLY PLAY")&&(!name.Contains("Jaina Proudmoore")))
                                        {
                                            string output =
                                                string.Format("[+] Card Moved - NAME: {0} ID: {1} FROM: {2} TO: {3}",
                                                    name, id, @from, to);
                                            if (m_AllCards.ContainsKey(id))
                                            {
                                                fireCardPlayedEvent(output);
                                                fireCardPlayedEvent(JsonConvert.SerializeObject(m_AllCards[id]));
                                            }
                                        }
                                    }
                                    else
                                    {
                                        Thread.Sleep(m_delay);
                                        continue;
                                    }
                                }
                            }
                            Thread.Sleep(m_delay);
                        }
                    }

                });
            bw.RunWorkerAsync();
        }

        // Fired each time a card is played.
        public event Action<object> CardPlayedEvent;
        private void fireCardPlayedEvent(string msg)
        {

            if (CardPlayedEvent != null)
            {
                CardPlayedEventArgs e = new CardPlayedEventArgs { CardJSON = msg };
                CardPlayedEvent(e);
            }
        }


        public void getMyDeck(Action<object> callback)
        {
            int i = 0;
            foreach (var entry in m_AllCards)
            {
                i++;
                m_MyDeck.Add(entry.Value);
                // do something with entry.Value or entry.Key
                if (i > 30)
                    break;
            }

            callback(JsonConvert.SerializeObject(m_MyDeck[0]));
        }



        public class ThresholdReachedEventArgs : EventArgs
        {
            public string s1 { get; set; }

        }
        /// <summary>
        /// Cleans up any resources being used.
        /// </summary>
        /// <skip/>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
    public class CardPlayedEventArgs : EventArgs
    {
        public string CardJSON { get; set; }
    }
}
