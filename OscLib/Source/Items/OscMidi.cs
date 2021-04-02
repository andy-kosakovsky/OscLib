using System;
using System.Collections.Generic;
using System.Text;

namespace OscLib
{
    /// <summary>
    /// Implements a MIDI message sent over the OSC Protocol.
    /// </summary>
    public readonly struct OscMidi
    {
        // TODO: FINISH THIS
        private readonly byte _portId;

        private readonly byte _status;

        private readonly byte _data1;

        private readonly byte _data2;


        public byte PortID { get => _portId; }
        public byte Status { get => _status; }
        public byte Data1 { get => _data1; }
        public byte Data2 { get => _data2; }

    }
}
