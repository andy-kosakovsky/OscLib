using System;
using System.Collections.Generic;
using System.Text;

namespace OscLib
{
    public readonly struct OscColor
    {
        public readonly byte Red;
        public readonly byte Green;
        public readonly byte Blue;
        public readonly byte Alpha;


        public OscColor(byte red, byte green, byte blue, byte alpha)
        {
            Red = red;
            Green = green;
            Blue = blue;
            Alpha = alpha;
        }


        public override string ToString()
        {
            return "OSC Color: [Red: " + Red + ", Green: " + Green + ", Blue: " + Blue + ", Alpha: " + Alpha + "]";
        }

    }

}
