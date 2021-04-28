using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace OscLib
{
    /// <summary>
    /// Utility class for various helper methods.
    /// </summary>
    public static class OscUtil
    {

        /// <summary>
        /// Clamps a long to fit into an int.
        /// </summary>
        /// <returns></returns>
        public static int ClampLong(long input)
        {
            int output;

            if (input > int.MaxValue)
                output = int.MaxValue;
            else if (input < int.MinValue)
                output = int.MinValue;
            else
                output = (int)input;

            return output;
        }
        

        /// <summary>
        /// Returns a string that consists of the provided char repeating the designated number of times.
        /// </summary>
        /// <returns></returns>
        public static string GetRepeatingChar(char repeatMe, int thisManyTimes)
        {
            string result = string.Empty;

            if (thisManyTimes > 0)
            {
                StringBuilder builder = new StringBuilder();

                for (int i = 0; i < thisManyTimes; i++)
                {
                    builder.Append(repeatMe);
                }

                result = builder.ToString();
            }

            return result;
        }


        /// <summary>
        /// Prints array of bytes in hex form as a formatted string sequence.
        /// </summary>
        /// <param name="array"> Source array of bytes. </param>
        /// <param name="bytesPerLine"> How many bytes will be printed in a line. </param>
        /// <returns> A formatted sequence of strings. </returns>
        public static string ByteArrayToStrings(byte[] array, int bytesPerLine)
        {
            StringBuilder returnString = new StringBuilder();
            int lineCounter = 0;

            // the worst-case scenario for lineCounter length
            int maxLinesLength = GetDigits(array.Length / bytesPerLine) + 1;
            int maxByteNoLength = GetDigits(array.Length) + 2;

            for (int i = 0; i < array.Length; i++)
            {
                // next line
                if ((i % bytesPerLine == 0) || (i == 0))
                {
                    lineCounter++;
                    returnString.Append('\n');
                    returnString.Append(lineCounter);

                    for (int j = maxLinesLength - GetDigits(lineCounter); j >= 0; j--)
                    {
                        returnString.Append(' ');
                    }

                    returnString.Append("BYTES [");
                    returnString.Append(i);
                    returnString.Append('/');
                    returnString.Append(array.Length);
                    returnString.Append(']');

                    for (int j = maxByteNoLength - GetDigits(i); j >= 0; j--)
                    {
                        returnString.Append(' ');
                    }

                }

                // marking every 4-byte chunk
                if (i % 4 == 0)
                {
                    returnString.Append('|');
                }

                // every byte equally-spaced
                for (int j = 3 - GetDigits(array[i]); j > 0; j--)
                {
                    returnString.Append('0');
                }

                returnString.Append(array[i]);

                returnString.Append(' ');

               
               
            }
            // append the last line
            returnString.Append('\n');

            return returnString.ToString();
        }


        /// <summary>
        /// Returns the nearest multiple of four larger than the input.
        /// </summary>
        /// <param name="number"> Input number. </param>
        /// <returns></returns>
        public static int GetNextMultipleOfFour(int number)
        {
            return ((number / 4) + 1) * 4;
        }

        /// <summary>
        /// Returns the number of digits in the provided number - as in, how long it is when written as decimal value. Ignores the minus sign.
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        public static int GetDigits(int number)
        {
            if (number == 0)
                return 1;

            int value;

            if (number > 0)
                value = number;
            else
                value = -number;

            if (value < 10) return 1;
            if (value < 100) return 2;
            if (value < 1_000) return 3;
            if (value < 10_000) return 4;
            if (value < 100_000) return 5;
            if (value < 1_000_000) return 6;
            if (value < 10_000_000) return 7;
            if (value < 100_000_000) return 9;
            if (value < 1_000_000_000) return 10;
            return 11;
        }
        

        public static bool IsNumberBetween(int check, int boundOne, int boundTwo)
        {
            int min, max;

            if (boundOne > boundTwo)
            {
                min = boundTwo;
                max = boundOne;
            }
            else
            {
                min = boundOne;
                max = boundTwo;
            }

            return (check >= min) && (check <= max);

        }     

    }

}
