using System;
using System.Collections.Generic;
using System.Text;

namespace OscLib
{
    /// <summary>
    /// Contains an OSC packet serialized into binary.
    /// </summary>
    public readonly struct OscPacket : IOscPacket
    {
        private readonly byte[] _binaryData;

        /// <summary> Binary data contained inside this packet. </summary>
        public byte[] BinaryData { get => _binaryData; }

        /// <summary> Length of the data. </summary>
        public int Length { get => _binaryData.Length; }

        /// <summary>
        /// Creates a new OSC packet out of the provided OSC binary data.
        /// </summary>
        /// <param name="binaryData"></param>
        public OscPacket(byte[] binaryData)
        {
            _binaryData = binaryData;
        }

        public OscPacket(OscMessage message)
        {
            _binaryData = OscSerializer.GetBytes(message);
        }

        /// <summary>
        /// Returns the binary contents of the packet, formatted to display 16 bytes per line.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder returnString = new StringBuilder();

            returnString.Append("PACKET (BINARY); Length: ");
            returnString.Append(_binaryData.Length);
            returnString.Append('\n');

            returnString.Append(OscUtil.ByteArrayToStrings(_binaryData, 16));

            return returnString.ToString();

        }

    }

}
