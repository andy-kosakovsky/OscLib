using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace OscLib
{
   /// <summary>
   /// A basic implementation of a yes-no-maybe boolean-like thing. 
   /// </summary>
    internal enum Trit
    {
        True = 1,
        False = -1,
        Maybe = 0
    }


    /// <summary>
    /// Utility class for various helper methods.
    /// </summary>
    public static class OscUtil
    {

        /// <summary> Returns an end point at a local address on a random open port. </summary> 
        public static IPEndPoint GetLocalEndPoint()
        { 
            return new IPEndPoint(OscProtocol.LocalIP, 0); 
        } 


        public static IPEndPoint GetLocalEndPointWithPort(int port)
        {
            return new IPEndPoint(OscProtocol.LocalIP, port);
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

            byte[] converterArray = new byte[bytesPerLine];
            int lineCounter = 0;

            for (int i = 0; i < array.Length; i++)
            {
                if ((i > 0) && (i % bytesPerLine == 0))
                {
                    lineCounter += bytesPerLine;
                    returnString.Append('\n');
                    returnString.Append(BitConverter.ToString(converterArray));
                    Array.Clear(converterArray, 0, bytesPerLine);
                }

                converterArray[i - lineCounter] = array[i];
            }
            // append the last line

            returnString.Append('\n');
            returnString.Append(BitConverter.ToString(converterArray));
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
        /// Swaps the endianness of the provided array of bytes.
        /// </summary>
        /// <param name="data"> Array of bytes to be swapped (needs to be of an even length to work). </param>
        internal static void SwapEndian(byte[] data)
        {
            if (data.Length % 2 != 0)
            {
                throw new ArgumentException("ERROR: Can't swap endianness, provided byte array is not of an even length (length is " + data.Length + ").");
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
        /// <param name="length"> How many bytees to swap around (needs to be even). </param>
        public static void SwapEndian(byte[] data, int startIndex, int length)
        {
            if (length % 2 != 0)
            {
                throw new ArgumentException("ERROR: Can't swap endianness, provided length is not even (length is " + length + ").");
            }

            for (int i = 0; i < (length / 2); i++)
            {
                byte currentValue = data[startIndex + i];
                data[startIndex + i] = data[startIndex + length - 1 - i];
                data[startIndex + length - 1 - i] = currentValue;
            }
           
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
