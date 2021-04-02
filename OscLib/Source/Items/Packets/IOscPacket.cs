
namespace OscLib
{ 
    /// <summary>
    /// Describes a structure that contains an OSC packet in binary form.
    /// </summary>
    public interface IOscPacket
    {
        /// <summary> OSC-compliant binary data contained within the packet. </summary>
        byte[] BinaryData { get; }

        /// <summary> Packet length in bytes. </summary>
        int OscLength { get; }

    }

}

