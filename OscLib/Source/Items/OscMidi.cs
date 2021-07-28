
namespace OscLib
{
    /// <summary>
    /// Represents a MIDI message, as per OSC Protocol specification. 
    /// </summary>
    /// <remarks> Intended to be used as an argument in OSC Messages. </remarks>
    public readonly struct OscMidi : IOscBlobbable
    {
        /// <summary> The port ID. </summary>
        public readonly byte PortId;

        /// <summary> The status byte. </summary>
        public readonly byte Status;

        /// <summary> The first data byte. </summary>
        public readonly byte Data1;

        /// <summary> The second data byte. </summary>
        public readonly byte Data2;

        /// <summary> Returns the total size of this struct when represented as an OSC-blob. </summary>
        public int SizeAsBlob { get => 8; }


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
        /// Adds this <see cref="OscMidi"/> struct, formatted as an OSC-blob, to the provided byte array at the specified position. Moves the pointer forward. 
        /// </summary>
        public void AddAsBlob(byte[] array, ref int extPointer)
        {
            OscSerializer.AddBytes(OscProtocol.Chunk32, array, ref extPointer);
            OscSerializer.AddBytes(this, array, ref extPointer);
        }


        /// <summary>
        /// Adds this <see cref="OscMidi"/> struct, formatted as an OSC-blob, to the provided byte array at the specified position.  
        /// </summary>
        public void AddAsBlob(byte[] array, int pointer)
        {
            int extPointer = pointer;
            AddAsBlob(array, ref extPointer);
        }


        /// <summary>
        /// Returns this <see cref="OscMidi"/> struct as a properly-formatted OSC-blob.
        /// </summary>
        public byte[] GetAsBlob()
        {
            byte[] returnArray = new byte[8];
            AddAsBlob(returnArray, 0);

            return returnArray;
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
