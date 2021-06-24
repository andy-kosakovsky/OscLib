using System;
using System.Runtime.InteropServices;

namespace OscLib
{
    /// <summary>
    /// Contains functionality for swapping the endianness of data.
    /// </summary>
    public static class OscEndian
    {
        // an implementation of a neat little trick that i found in a blogpost by Raymond Chen.
        // allows to pretend that floats are integers and vice versa, as far as their byte content goes
        // avoids having to use unsafe code
        [StructLayout(LayoutKind.Explicit)]
        private struct LongAndDouble
        {
            [FieldOffset(0)] public long LongValue;
            [FieldOffset(0)] public double DoubleValue;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct IntAndFloat
        {
            [FieldOffset(0)] public int IntValue;
            [FieldOffset(0)] public float FloatValue;
        }


        #region SWAPPING ENDIANNESS WITH BYTE ARRAYS
        /// <summary>
        /// Swaps the endianness of the provided array of bytes.
        /// </summary>
        /// <remarks> This method will swap the data around *inside* the provided array - it doesn't return a copy. </remarks>
        /// <param name="data"> The target array (has to be of an even length). </param>
        public static void Swap(byte[] data)
        {
            if (data.Length % 2 != 0)
            {
                throw new ArgumentException("ERROR: Can't swap endianness, provided byte array is not of an even length (length is " + data.Length.ToString() + ").");
            }

            for (int i = 0; i < data.Length / 2; i++)
            {
                byte currentValue = data[i];
                data[i] = data[data.Length - 1 - i];
                data[data.Length - 1 - i] = currentValue;
            }

        }


        /// <summary>
        /// Swaps the endiannes of *some* data within the provided byte array.
        /// </summary>
        /// <param name="data"> The target array. </param>
        /// <param name="startIndex"> The index from which to start the swapping. </param>
        /// <param name="length"> How many bytes to swap around (has to be an even number). </param>
        public static void Swap(byte[] data, int startIndex, int length)
        {
            if (length % 2 != 0)
            {
                throw new ArgumentException("ERROR: Can't swap endianness, provided length is not even (length is " + length.ToString() + ").");
            }

            for (int i = 0; i < (length / 2); i++)
            {
                byte currentValue = data[startIndex + i];
                data[startIndex + i] = data[startIndex + length - 1 - i];
                data[startIndex + length - 1 - i] = currentValue;
            }

        }

        #endregion // SWAPPING ENDIANNESS BY BYTE ARRAY


        #region SWAPPING ENDIANNESS BY TYPE
        // a quick-n-dirty way to swap endianness of all commonly-encountered types while avoiding going to heap or fiddling with arrays
        // kinda stupid and kinda WET but oh well. guess i like typing and copypasting

        /// <summary>
        /// Swaps the endianness of the specified 32-bit unsigned integer.
        /// </summary>
        /// <returns> A 32-bit unsigned integer - encoding the same value as the original but with its bytes the other way around. </returns>
        public static uint Swap(uint input)
        {
            uint byte_1 = (input & 0x000000FF);
            uint byte_2 = (input & 0x0000FF00) >> 8;
            uint byte_3 = (input & 0x00FF0000) >> 16;
            uint byte_4 = (input & 0xFF000000) >> 24;

            byte_1 <<= 24;
            byte_2 <<= 16;
            byte_3 <<= 8;

            return byte_1 | byte_2 | byte_3 | byte_4;
        }


        /// <summary>
        /// Swaps the endianness of the specified 32-bit signed integer.
        /// </summary>
        /// <returns> A 32-bit signed integer - encoding the same value as the original but with its bytes the other way around. </returns>
        public static int Swap(int input)
        {
            uint uinput = (uint)input;

            uinput = Swap(uinput);

            return (int)uinput;
        }


        /// <summary>
        /// Swaps the endianness of the specified 32-bit floating-point value.
        /// </summary>
        /// <returns> A 32-bit floating-point value - encoding the same value as the original but with its bytes the other way around. </returns>
        public static float Swap(float input)
        {
            IntAndFloat intAndFloat = new IntAndFloat
            {
                FloatValue = input
            };

            intAndFloat.IntValue = Swap(intAndFloat.IntValue);

            return intAndFloat.FloatValue;
        }

        /// <summary>
        /// Swaps the endianness of the specified 64-bit unsigned integer.
        /// </summary>
        /// <returns> A 64-bit unsigned integer - encoding the same value as the original but with its bytes the other way around. </returns>
        public static ulong Swap(ulong input)
        {
            ulong byte_1 = (input & 0x0000_0000_0000_00FF);
            ulong byte_2 = (input & 0x0000_0000_0000_FF00) >> 8;
            ulong byte_3 = (input & 0x0000_0000_00FF_0000) >> 16;
            ulong byte_4 = (input & 0x0000_0000_FF00_0000) >> 24;

            ulong byte_5 = (input & 0x0000_00FF_0000_0000) >> 32;
            ulong byte_6 = (input & 0x0000_FF00_0000_0000) >> 40;
            ulong byte_7 = (input & 0x00FF_0000_0000_0000) >> 48;
            ulong byte_8 = (input & 0xFF00_0000_0000_0000) >> 56;


            byte_1 <<= 56;
            byte_2 <<= 48;
            byte_3 <<= 40;
            byte_4 <<= 32;

            byte_5 <<= 24;
            byte_6 <<= 16;
            byte_7 <<= 8;

            return byte_1 | byte_2 | byte_3 | byte_4 | byte_5 | byte_6 | byte_7 | byte_8;
        }


        /// <summary>
        /// Swaps the endianness of the specified 64-bit signed integer (also known as long).
        /// </summary>
        /// <returns> A 64-bit integer - encoding the same value as the original but with its bytes the other way around. </returns>
        public static long Swap(long input)
        {
            ulong uinput = (ulong)input;

            uinput = Swap(uinput);

            return (long)uinput;
        }


        /// <summary>
        /// Swaps the endianness of the specified 64-bit floating-point value.
        /// </summary>
        /// <returns> A 64-bit floating-point value - encoding the same value as the original but with its bytes the other way around. </returns>
        public static double Swap(double input)
        {
            LongAndDouble longAndDouble = new LongAndDouble
            {
                DoubleValue = input
            };

            longAndDouble.LongValue = Swap(longAndDouble.LongValue);

            return longAndDouble.DoubleValue;
        }

        #endregion

    }

}
