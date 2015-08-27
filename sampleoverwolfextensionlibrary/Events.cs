using System;
using System.Collections.Generic;
using System.Text;

namespace SampleOverwolfExtensionLibrary.Events
{
        public class CardReceivedEventArgs : EventArgs
        {
            public string CardJSON { get; set; }
        }
        public class CardPlayedEventArgs : EventArgs
        {
            public string CardJSON { get; set; }
        }

        public class OpponentCardPlayedEventArgs : EventArgs
        {
            public string CardJSON { get; set; }
        }
}
