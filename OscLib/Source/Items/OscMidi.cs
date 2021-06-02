using System;
using System.Collections.Generic;
using System.Text;

namespace OscLib
{
    /// <summary>
    /// Represents a MIDI message, as per OSC Protocol specification. 
    /// </summary>
    public readonly struct OscMidi
    {
        /// <summary> The port ID. </summary>
        public readonly byte PortId;

        /// <summary> The status byte. </summary>
        public readonly byte Status;

        /// <summary> The first data byte. </summary>
        public readonly byte Data1;

        /// <summary> The second data byte. </summary>
        public readonly byte Data2;

        /// <summary>
        /// Creates a new MIDI message struct that can interact with OSC.
        /// </summary>
        public OscMidi(byte portId, byte status, byte data1, byte data2)
        {
            PortId = portId;
            Status = status;
            Data1 = data1;
            Data2 = data2;
        }

        /// <summary>
        /// Returns this struct formatted as a string.
        /// </summary>
        public override string ToString()
        {
            return "OSC MIDI: [Port ID: " + PortId.ToString() + ", Status: " + Status.ToString() + ", Data Byte 1: " + Data1.ToString() + ", Data Byte 2: " + Data2.ToString() + "]";
        }

    }
}
