using System;
using System.Collections.Generic;
using System.Text;

namespace SampleOverwolfExtensionLibrary
{
    public class Card
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public string Rarity { get; set; }
        public string Type { get; set; }
        public int Attack { get; set; }
        public int Health { get; set; }
        public string Cost { get; set; }
        public string Faction { get; set; }
        public string Text { get; set; }
        public IList<string> Mechanics { get; set; }
        public string Played { get; set; }
        public string Flavor { get; set; }
        public string Artist { get; set; }
        public bool Collectible { get; set; }
        public bool Elite { get; set; }
    }
}
