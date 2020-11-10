using System;
using System.Collections.Generic;
using System.Text;

namespace OscLib
{
    /// <summary>
    /// Contains serialized binary data that makes up an OSC packet. 
    /// </summary>
    public readonly struct OscPacketBinary : IOscPacketBinary
    {
        private readonly byte[] _binaryData;
        private readonly int _length;

        /// <summary> Binary data contained inside this packet. </summary>
        public byte[] BinaryData { get => _binaryData; }

        /// <summary> Length of the data. </summary>
        public int Length { get => _length; }

        /// <summary>
        /// Creates a new OSC packet out of the provided binary data.
        /// </summary>
        /// <param name="binaryData"></param>
        public OscPacketBinary(byte[] binaryData)
        {
            _length = binaryData.Length;
            _binaryData = binaryData;

        }

        /// <summary>
        /// Returns the binary contents of the packet, formatted to display 16 bytes per line.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder returnString = new StringBuilder();

            returnString.Append("PACKET (BINARY); Length: ");
            returnString.Append(_length);
            returnString.Append('\n');

            returnString.Append(OscUtil.ByteArrayToStrings(_binaryData, 16));

            return returnString.ToString();

        }

    }

}
