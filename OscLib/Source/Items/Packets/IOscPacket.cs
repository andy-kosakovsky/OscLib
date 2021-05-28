
namespace OscLib
{ 
    /// <summary>
    /// Describes a structure that contains a packet of OSC binary data.
    /// </summary>
    public interface IOscPacket
    {
        /// <summary> Packet length in bytes. Per OSC Protocol spec, should be a multiple of 4. </summary>
        int OscLength { get; }

        /// <summary>
        /// Indexer access to the binary data inside this packet.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        byte this[int index] { get; }

        /// <summary>
        /// Returns the the OSC binary data in this packet as a byte array.
        /// </summary>
        /// <returns></returns>
        byte[] GetBytes();

        /// <summary>
        /// Copies the contents of this packet to the provided byte array.
        /// </summary>
        /// <param name="target"> The target array to copy to. </param>
        /// <param name="index"> The index to which to copy. </param>
        void CopyToByteArray(byte[] target, int index);

    }

}

