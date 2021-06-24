using System;
using System.Collections.Generic;
using System.Text;

namespace OscLib
{
    /// <summary>
    /// Represents a packet of OSC data serialized into bytes.
    /// </summary>
    /// <remarks>
    /// The basic purpose of this struct is to clearly designate a byte array as containing OSC binary data. 
    /// Nothing more, nothing less - if a byte array is inside this struct, it should be safe to use with anything OSC-related.
    /// IOscPacket interface can be used to create custom packets, should extra functionality be needed.
    /// </remarks>
    public readonly struct OscPacket : IOscPacket
    {
        private readonly byte[] _contents;

        /// <summary> The number of bytes contained in this Packet. </summary>
        public int Size { get => _contents.Length; }

        /// <summary>
        /// Provides indexer access to binary data inside this packet.
        /// </summary>
        /// <remarks> This should help avoid accidents with overwriting bytes in the supposedly "read-only" byte array. The whole array can still 
        /// be retrieved via the appropriate method, just in case it's needed. </remarks>
        /// <param name="index"> Byte index. </param>
        /// <returns> One byte of data present at the specified index, or 0 if the index is out of bounds. </returns>
        public byte this[int index]
        {
            get
            {
                if ((index < 0) || (index >= _contents.Length))
                {
                    return 0;
                }    
                else
                {
                    return _contents[index];
                }

            }

        }


        /// <summary>
        /// Creates a new OSC Packet out of the provided byte array containing OSC data (hopefully).
        /// </summary>
        /// <param name="data"> Should contain valid OSC binary data. </param>
        public OscPacket(byte[] data)
        {
            if (!data.IsValidOscData())
            {
                throw new ArgumentException("OSC Packet ERROR: Cannot create new OscPacket, provided binary data is not valid OSC data. ");
            }

            _contents = data;

        }


        /// <summary>
        /// Same as the other constructor, but doesn't validate anything, assuming it's been done already.
        /// </summary>
        internal OscPacket(byte[] data, bool isValid)
        {
            _contents = data;
        }


        /// <summary>
        /// Creates a new OSC Packet out of a part of the provided byte array containing OSC data (hopefully).
        /// </summary>
        /// <param name="dataSource"> Should contain valid OSC binary data for the relevant length. </param>
        /// <param name="index"> The index from which to relevant part of the byte array begins. </param>
        /// <param name="length"> The length of the relevant part of the byte array. </param>
        public OscPacket(byte[] dataSource, int index, int length)
        {
            if (!dataSource.IsValidOscData(index, length))
            {
                throw new ArgumentException("OSC Packet ERROR: Cannot create new OscPacket, provided binary data isn't valid OSC data. ");
            }

            _contents = new byte[length];
            Array.Copy(dataSource, index, _contents, 0, length);
        }


        /// <summary>
        /// Same as the other constructor, but doesn't validate anything, assuming it's been done already.
        /// </summary>
        internal OscPacket(byte[] dataSource, int index, int length, bool isValid)
        {
            _contents = new byte[length];
            Array.Copy(dataSource, index, _contents, 0, length);
        }


        /// <summary>
        /// Retrieves the byte array containing OSC data from this packet.
        /// </summary>
        /// <remarks> Despite being read-only, one can still change individual values inside this array. This can be both good and bad - and definitely something to be wary of.
        /// If this behaviour is not explicitly needed, it's probably safer to use the indexer access instead. </remarks>
        public byte[] GetContents()
        {
            return _contents;
        }


        /// <summary>
        /// Returns a copy of the byte array containing OSC data.
        /// </summary>
        public byte[] GetCopyOfContents()
        {
            byte[] copy = new byte[_contents.Length];
            _contents.CopyTo(copy, 0);

            return copy;
        }


        /// <summary>
        /// Copies the contents of this packet to the provided byte array.
        /// </summary>
        /// <param name="target"> The target array to copy to. </param>
        /// <param name="index"> The index to which to copy. </param>
        public void CopyContentsToArray(byte[] target, int index)
        {
            _contents.CopyTo(target, index);
        }


        /// <summary>
        /// Returns the binary contents of the packet, formatted to display 16 bytes per line.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder returnString = new StringBuilder();

            returnString.Append("PACKET (BINARY); Length: ");
            returnString.Append(_contents.Length);
            returnString.Append("; Contents: ");
            returnString.Append('\n');

            returnString.Append(OscUtil.ByteArrayToStrings(_contents, 16));

            return returnString.ToString();

        }

    }

}
