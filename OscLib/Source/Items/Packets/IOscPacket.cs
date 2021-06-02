
namespace OscLib
{ 
    /// <summary>
    /// Describes a packet of OSC binary data.
    /// </summary>
    public interface IOscPacket : IBinaryContainer
    {
        /// <summary> Length of binary data, in bytes. Should be a multiple of 4. </summary>
        int Length { get; }
    }

}

