using System;
using System.Collections.Generic;
using System.Text;

namespace SampleOverwolfExtensionLibrary.Events
{
    public class OverwolfEventArgs : EventArgs
    {
    }
    public class CardReceivedEventArgs : OverwolfEventArgs
    {
        public string CardJSON { get; set; }
    }
    public class CardPlayedEventArgs : OverwolfEventArgs
    {
        public string CardJSON { get; set; }
    }

    public class OpponentCardPlayedEventArgs : OverwolfEventArgs
    {
        public string CardJSON { get; set; }
    }

    public class ErrorEventArgs : OverwolfEventArgs
    {
        public string Message { get; set; }
    }

    public class FatalErrorEventArgs : OverwolfEventArgs
    {
        public string Message { get; set; }
    }
    public class UpdateCompleteEventArgs : OverwolfEventArgs
    {
        public string Message { get; set; }
    }
}
