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
        public readonly byte PortId;
        public readonly byte Status;
        public readonly byte Data1;
        public readonly byte Data2;

        
        public OscMidi(byte portId, byte status, byte data1, byte data2)
        {
            PortId = portId;
            Status = status;
            Data1 = data1;
            Data2 = data2;
        }

        public override string ToString()
        {
            return "OSC MIDI: [Port ID: " + PortId + ", Status: " + Status + ", Data Byte 1: " + Data1 + ", Data Byte 2: " + Data2 + "]";
        }

    }
}
