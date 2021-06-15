using System;
using System.Collections.Generic;
using System.Text;

namespace OscLib
{
    /// <summary>
    /// Represents a packet of OSC data that was serialized into bytes.
    /// </summary>
    /// <remarks>
    /// The basic purpose of this struct is to clearly designate a byte array as containing OSC binary data. 
    /// Nothing more, nothing less - if a byte array is wrapped by this struct, it should be safe to use with anything OSC-related.
    /// IOscPacket interface can be used to create custom packets should extra functionality be needed.
    /// </remarks>
    public readonly struct OscPacket : IOscPacket
    {
        private readonly byte[] _binaryData;

        /// <summary> Length of the data. </summary>
        public int Length { get => _binaryData.Length; }

        /// <summary>
        /// Provides indexer access to the data inside this packet.
        /// </summary>
        /// <remarks> This should help avoid accidents with overwriting bytes in the supposedly "read-only" byte array. The whole array can still 
        /// be retrieved via the appropriate method, just in case it's needed. </remarks>
        /// <param name="index"> Byte index. </param>
        /// <returns> One byte of data, or 0 if it's out of bounds. </returns>
        public byte this[int index]
        {
            get
            {
                if ((index < 0) || (index >= _binaryData.Length))
                {
                    return 0;
                }    
                else
                {
                    return _binaryData[index];
                }

            }

        }


        /// <summary>
        /// Creates a new OSC Packet out of the provided byte array containing OSC data (hopefully).
        /// </summary>
        /// <param name="data"> Should contain valid OSC binary data. </param>
        /// <remarks> This constructor does VERY minimal validation - it only checks the first byte and whether the total length is a multiple of 4. Use at your own risk. </remarks>
        public OscPacket(byte[] data)
        {
            if ((data[0] != OscProtocol.BundleMarker) && (data[0] != OscProtocol.Separator))
            {
                throw new ArgumentException("OSC Packet ERROR: Cannot create new OSC Packet, provided binary data doesn't seem to be valid.");
            }

            if (data.Length % 4 != 0)
            {
                throw new ArgumentException("OSC Packet ERROR: Cannot create new OSC Packet, provided byte array's length is not a multiple of 4.");
            }

            _binaryData = data;
        }


        /// <summary>
        /// Creates a new OSC Packet out of a part of the provided byte array containing OSC data (hopefully).
        /// </summary>
        /// <param name="dataSource"> Should contain valid OSC binary data for the relevant length. </param>
        /// <param name="index"> The index from which to relevant part of the byte array begins. </param>
        /// <param name="length"> The length of the relevant part of the byte array. </param>
        /// <remarks> This constructor does VERY minimal validation - it only checks the first byte and whether the total length is a multiple of 4. Use at your own risk. </remarks>
        public OscPacket(byte[] dataSource, int index, int length)
        {
            if ((dataSource[index] != OscProtocol.BundleMarker) && (dataSource[0] != OscProtocol.Separator))
            {
                throw new ArgumentException("OSC Packet ERROR: Cannot create new OSC Packet, provided binary data doesn't seem to be valid.");
            }

            if (length % 4 != 0)
            {
                throw new ArgumentException("OSC Packet ERROR: Cannot create new OSC Packet, provided data's length is not a multiple of 4.");
            }

            _binaryData = new byte[length];
            Array.Copy(dataSource, index, _binaryData, 0, length);
        }


        /// <summary>
        /// Retrieves the byte array containing OSC data from this packet.
        /// </summary>
        /// <remarks> Despite being read-only, one can still change individual values inside this array. This can be both good and bad - and definitely something to be wary of.
        /// If this behaviour is not explicitly needed, it's probably safer to use the indexer access instead. </remarks>
        public byte[] GetBytes()
        {
            return _binaryData;
        }


        /// <summary>
        /// Returns a copy of the byte array containing OSC data.
        /// </summary>
        public byte[] GetCopyOfBytes()
        {
            byte[] copy = new byte[_binaryData.Length];
            _binaryData.CopyTo(copy, 0);

            return copy;
        }


        /// <summary>
        /// Copies the contents of this packet to the provided byte array.
        /// </summary>
        /// <param name="target"> The target array to copy to. </param>
        /// <param name="index"> The index to which to copy. </param>
        public void CopyBytesToArray(byte[] target, int index)
        {
            _binaryData.CopyTo(target, index);
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
