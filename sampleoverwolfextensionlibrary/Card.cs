using System;
using System.Collections.Generic;
using System.Text;

namespace SampleOverwolfExtensionLibrary
{
    [Serializable()]
    public class Card
    {
        [System.Xml.Serialization.XmlElement("CardId")]
        public string ID { get; set; }

        [System.Xml.Serialization.XmlElement("Name")]
        public string Name { get; set; }

        [System.Xml.Serialization.XmlElement("Rarity")]
        public string Rarity { get; set; }

        [System.Xml.Serialization.XmlElement("Type")]
        public string Type { get; set; }

        [System.Xml.Serialization.XmlElement("Attack")]
        public int Attack { get; set; }

        [System.Xml.Serialization.XmlElement("Health")]
        public int Health { get; set; }

        [System.Xml.Serialization.XmlElement("Cost")]
        public string Cost { get; set; }

        [System.Xml.Serialization.XmlElement("Faction")]
        public string Faction { get; set; }

        [System.Xml.Serialization.XmlElement("Text")]
        public string Text { get; set; }

        [System.Xml.Serialization.XmlElement("Mechanics")]
        public IList<string> Mechanics { get; set; }

        [System.Xml.Serialization.XmlElement("Played")]
        public string Played { get; set; }

        [System.Xml.Serialization.XmlElement("Played")]
        public string Flavor { get; set; }

        [System.Xml.Serialization.XmlElement("Played")]
        public string Artist { get; set; }

        [System.Xml.Serialization.XmlElement("Played")]
        public bool Collectible { get; set; }

        [System.Xml.Serialization.XmlElement("Played")]
        public bool Elite { get; set; }
    }
}
