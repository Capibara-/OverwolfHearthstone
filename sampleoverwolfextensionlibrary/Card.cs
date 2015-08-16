using System;
using System.Collections.Generic;
using System.Text;

namespace SampleOverwolfExtensionLibrary
{
    [Serializable()]
    public class Card
    {
        [System.Xml.Serialization.XmlElement("CardId")]
        public string CardId { get; set; }

        [System.Xml.Serialization.XmlElement("Name")]
        public string Name { get; set; }

        [System.Xml.Serialization.XmlElement("CardSet")]
        public string CardSet { get; set; }

        [System.Xml.Serialization.XmlElement("Rarity")]
        public string Rarity { get; set; }

        [System.Xml.Serialization.XmlElement("Type")]
        public string Type { get; set; }

        [System.Xml.Serialization.XmlElement("Attack")]
        public string Attack { get; set; }

        [System.Xml.Serialization.XmlElement("Health")]
        public string Health { get; set; }

        [System.Xml.Serialization.XmlElement("Cost")]
        public string Cost { get; set; }

        [System.Xml.Serialization.XmlElement("Durability")]
        public string Durability { get; set; }

        [System.Xml.Serialization.XmlElement("Class")]
        public string Class { get; set; }

        [System.Xml.Serialization.XmlElement("Faction")]
        public string Faction { get; set; }

        [System.Xml.Serialization.XmlElement("Text")]
        public string Text { get; set; }

        [System.Xml.Serialization.XmlElement("Mechanics")]
        public string Mechanics { get; set; }

        [System.Xml.Serialization.XmlElement("Played")]
        public string Played { get; set; }
    }
}
