using System;
using System.Collections.Generic;
using System.Text;

namespace OscLib
{
    /// <summary>
    /// Represents an OSC packet serialized into binary.
    /// </summary>
    /// <remarks>
    /// The purpose of this struct is to basically clearly designate a byte array as containing OSC binary data. 
    /// Nothing more, nothing less - if a byte array is wrapped inside this struct, it should be safe to use with anything OSC-related.
    /// </remarks>
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


        /// <summary>
        /// Serializes an OSC Message into bytes and creates an OSC Packet out of it.
        /// </summary>
        /// <param name="message"> The message to be serialized. </param>
        public OscPacket(OscMessage message)
        {
            _binaryData = OscSerializer.GetBytes(message);
        }


        /// <summary>
        /// Serializes an OSC Bundle into bytes and creats an OSC Packet out of it.
        /// </summary>
        /// <param name="bundle"></param>
        public OscPacket(OscBundle bundle)
        {
            _binaryData = OscSerializer.GetBytes(bundle);
        }


        /// <summary>
        /// Creates a message out of the provided address pattern and arguments, serializes it into bytes and creates an OSC Packet.
        /// </summary>
        /// <param name="addressPattern"></param>
        /// <param name="arguments"></param>
        public OscPacket(OscString addressPattern, object[] arguments = null)
        {
            _binaryData = OscSerializer.NewMessageGetBytes(addressPattern, arguments);
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
            returnString.Append("; Contents: ");
            returnString.Append('\n');

            returnString.Append(OscUtil.ByteArrayToStrings(_binaryData, 16));

            return returnString.ToString();

        }

    }

}
