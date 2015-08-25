using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SampleOverwolfExtensionLibrary
{
    class Deck
    {
        public Deck()
        {
            Cards = new List<Card>();
        }

        public Deck(IList<Card> cards)
        {
            Cards = cards;
        }
        
        public string Name { get; set; }
        public IList<Card> Cards { get; set; }
    }
}
