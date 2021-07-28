
namespace OscLib
{
    /// <summary>
    /// Represents an RGBA 8-bit color, as per OSC Protocol specification.
    /// </summary>
    /// <remarks> Intended to be used as an argument in OSC Messages. </remarks>
    public readonly struct OscColor : IOscBlobbable
    {
        /// <summary> Value of red channel. </summary>
        public readonly byte Red;

        /// <summary> Value of green channel. </summary>
        public readonly byte Green;

        /// <summary> Value of blue channel. </summary>
        public readonly byte Blue;

        /// <summary> Value of alpha channel. </summary>
        public readonly byte Alpha;

        /// <summary> Returns the total size of this struct when represented as an OSC-blob. </summary>
        public int SizeAsBlob { get => 8; }


        /// <summary>
        /// Creates a new struct containing OSC Color.
        /// </summary>
        public OscColor(byte red, byte green, byte blue, byte alpha)
        {
            Red = red;
            Green = green;
            Blue = blue;
            Alpha = alpha;
        }


        /// <summary>
        /// Adds this <see cref="OscColor"/> struct, formatted as an OSC-blob, to the provided byte array at the specified position. Moves the pointer forward. 
        /// </summary>
        public void AddAsBlob(byte[] array, ref int extPointer)
        {
            OscSerializer.AddBytes(OscProtocol.Chunk32, array, ref extPointer);
            OscSerializer.AddBytes(this, array, ref extPointer);
        }


        /// <summary>
        /// Adds this <see cref="OscColor"/> struct, formatted as an OSC-blob, to the provided byte array at the specified position.  
        /// </summary>
        public void AddAsBlob(byte[] array, int pointer)
        {
            int extPointer = pointer;
            AddAsBlob(array, ref extPointer);
        }


        /// <summary>
        /// Returns this <see cref="OscColor"/> struct as a properly-formatted OSC-blob.
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
            return "OSC Color: [Red: " + Red.ToString() + ", Green: " + Green.ToString() + ", Blue: " + Blue.ToString() + ", Alpha: " + Alpha.ToString() + "]";
        }

    }

}
