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
using Newtonsoft.Json.Linq;
using System.Configuration;
using System.Reflection;
using SampleOverwolfExtensionLibrary.Events;
using SampleOverwolfExtensionLibrary.OCR;

namespace SampleOverwolfExtensionLibrary
{
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
            string jsonPath = Configuration.Instance.JSONCardsFilePath;
            if (m_AllCards == null)
            {
                m_AllCards = new Dictionary<string, Card>();
                ParseCardsFromJSON(jsonPath);
            }

            BackgroundWorker bw = new BackgroundWorker();
            callback(string.Format("Initialized cards, count: {0}", m_AllCards.Count));
            bw.DoWork += new DoWorkEventHandler(bw_DoWork);
            bw.RunWorkerAsync();
        }

        private void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            string logFile = Configuration.Instance.GameLogFilePath;
            using (
                            FileStream fs = new FileStream(logFile, FileMode.Open, FileAccess.Read,
                                FileShare.ReadWrite))
            {
                m_lastOffset = FindLastGame(fs);
            }
            while (true)
            {

                using (
                    FileStream fs = new FileStream(logFile, FileMode.Open, FileAccess.Read,
                        FileShare.ReadWrite))
                {
                    fs.Seek(m_lastOffset, SeekOrigin.Begin);
                    if (fs.Length != m_lastOffset)
                    {
                        using (StreamReader sr = new StreamReader(fs))
                        {
                            string chunk = sr.ReadToEnd();
                            m_lastOffset = fs.Length;
                            var lines = chunk.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

                            foreach (string newLine in lines)
                            {
                                if (cardMovementRegex.IsMatch(newLine))
                                {
                                    //TODO add if (card is from -to our direction
                                    Match match = cardMovementRegex.Match(newLine);
                                    string l_id = match.Groups["Id"].Value.Trim();
                                    string l_name = match.Groups["name"].Value.Trim();
                                    string l_from = match.Groups["from"].Value.Trim();
                                    string l_to = match.Groups["to"].Value.Trim();
                                    //        if (l_id != "" && l_to.Contains("FRIENDLY HAND")&&(!l_name.Contains("Jaina Proudmoore")))
                                    if (m_AllCards.ContainsKey(l_id))
                                    {
                                        string output =
                                               string.Format("[+] Card Moved - NAME: {0} ID: {1} FROM: {2} TO: {3}",
                                                   l_name, l_id, l_from, l_to);
                                        if (l_id != "" && l_to.Contains("FRIENDLY HAND"))
                                        {
                                            //      fireCardPlayedEvent(output); 

                                            fireCardHandEvent(JsonConvert.SerializeObject(m_AllCards[l_id]));

                                        }
                                        if (l_id != "" && l_to.Contains("FRIENDLY PLAY") && l_id != "TU4a_006")
                                        {
                                            //      fireCardPlayedEvent(output); 
                                            m_AllCards[l_id].Played = "true";

                                            fireCardPlayedEvent(JsonConvert.SerializeObject(m_AllCards[l_id]));
                                        }

                                        if (l_id != "" && l_to.Contains("OPPOSING PLAY") && l_id != "TU4a_006")
                                        {
                                            m_AllCards[l_id].Played = "true";
                                            fireOpponentCardPlayedEvent(JsonConvert.SerializeObject(m_AllCards[l_id]));
                                        }
                                    }
                                }
                            }
                        }
                    }
                    Thread.Sleep(m_delay);
                }
            }
        }

        private void ParseCardsFromXML(string path)
        {
            XmlSerializer ser = new XmlSerializer(typeof(Configuration));
            Card c = null;
            ser = new XmlSerializer(typeof(Card));
            XmlDocument doc = new XmlDocument();

            doc = new XmlDocument();
            doc.Load(path);
            XmlElement root = doc.DocumentElement;
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
                                m_AllCards.Add(c.ID, c);


                            }
                            catch (Exception e)
                            {
                                // TODO: Handle bad XML file.
                            }
                        }
                    }
            }
        }

        private void ParseCardsFromJSON(string path)
        {
            var dynObj = JsonConvert.DeserializeObject<JObject>(File.ReadAllText(path));
            foreach (JToken token in dynObj.Children())
            {
                if (token is JProperty)
                {
                    var cards = JsonConvert.DeserializeObject<List<Card>>(token.First.ToString());
                    foreach (Card card in cards)
                    {
                        if (card.ID != null)
                        {
                            card.Played = "false";
                            m_AllCards.Add(card.ID, card);
                        }
                    }
                }
            }
        }

        private long FindLastGame(FileStream fs)
        {
            using (var sr = new StreamReader(fs))
            {
                bool foundSpectatorStart = false;
                long offset = 0, tempOffset = 0;
                var lines = sr.ReadToEnd().Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var line in lines)
                {
                    if (line.Contains("Begin Spectating") || line.Contains("Start Spectator"))
                    {
                        offset = tempOffset;
                        foundSpectatorStart = true;
                    }
                    else if (line.Contains("End Spectator"))
                        offset = tempOffset;
                    else if (line.Contains("CREATE_GAME"))
                    {
                        if (foundSpectatorStart)
                        {
                            foundSpectatorStart = false;
                            continue;
                        }
                        offset = tempOffset;
                        continue;
                    }
                    tempOffset += line.Length + 1;
                    if (line.StartsWith("[Bob] legend rank"))
                    {
                        if (foundSpectatorStart)
                        {
                            foundSpectatorStart = false;
                            continue;
                        }
                        offset = tempOffset;
                    }
                }

                return offset;
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


        public event Action<object> CardPlayedEvent;
        private void fireCardPlayedEvent(string msg)
        {

            if (CardPlayedEvent != null)
            {
                CardPlayedEventArgs e = new CardPlayedEventArgs { CardJSON = msg };
                CardPlayedEvent(e);
            }
        }

        public event Action<object> CardHandEvent;
        private void fireCardHandEvent(string msg)
        {

            if (CardHandEvent != null)
            {
                CardHandEventArgs e = new CardHandEventArgs { CardJSON = msg };
                CardHandEvent(e);
            }
        }

        public event Action<object> OpponentCardPlayedEvent;
        private void fireOpponentCardPlayedEvent(string msg)
        {
            if (OpponentCardPlayedEvent != null)
            {
                OpponentCardPlayedEventArgs e = new OpponentCardPlayedEventArgs { CardJSON = msg };
                OpponentCardPlayedEvent(e);
            }
        }


        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
