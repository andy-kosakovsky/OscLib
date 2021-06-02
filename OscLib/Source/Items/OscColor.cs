using System;
using System.Collections.Generic;
using System.Text;

namespace OscLib
{
    /// <summary>
    /// Represents an RGBA 8-bit color, as per OSC Protocol specification.
    /// </summary>
    public readonly struct OscColor
    {
        /// <summary> Value of red channel. </summary>
        public readonly byte Red;

        /// <summary> Value of green channel. </summary>
        public readonly byte Green;

        /// <summary> Value of blue channel. </summary>
        public readonly byte Blue;

        /// <summary> Value of alpha channel. </summary>
        public readonly byte Alpha;

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
        /// Returns this struct formatted as a string.
        /// </summary>
        public override string ToString()
        {
            return "OSC Color: [Red: " + Red.ToString() + ", Green: " + Green.ToString() + ", Blue: " + Blue.ToString() + ", Alpha: " + Alpha.ToString() + "]";
        }

    }

}
