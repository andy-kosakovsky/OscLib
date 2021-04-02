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
        public int OscLength { get => _binaryData.Length; }


        /// <summary>
        /// Creates a new OSC Packet out of the provided byte array containing OSC data (hopefully).
        /// </summary>
        /// <param name="data"> Should contain valid OSC binary data. This constructor does VERY minimal validation, so use at your own risk. </param>
        public OscPacket(byte[] data)
        {
            if ((data[0] != OscProtocol.BundleMarker) && (data[0] != OscProtocol.Separator))
            {
                throw new ArgumentException("OSC Packet ERROR: Cannot create new OSC Packet, provided binary data doesn't seem to be valid. ");
            }

            _binaryData = data;
        }


        /// <summary>
        /// Creates a new OSC Packet out of a part of the provided byte array containing OSC data (hopefully).
        /// </summary>
        /// <param name="dataSource"> Should contain valid OSC binary data for the relevant length. This constructor does VERY minimal validation, so use at your own risk. </param>
        /// <param name="index"> The index from which to relevant part of the byte array begins. </param>
        /// <param name="length"> The length of the relevant part of the byte array. </param>
        public OscPacket(byte[] dataSource, int index, int length)
        {
            if ((dataSource[index] != OscProtocol.BundleMarker) && (dataSource[0] != OscProtocol.Separator))
            {
                throw new ArgumentException("OSC Packet ERROR: Cannot create new OSC Packet, provided binary data doesn't seem to be valid. ");
            }

            _binaryData = new byte[length];
            Array.Copy(dataSource, index, _binaryData, 0, length);
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
