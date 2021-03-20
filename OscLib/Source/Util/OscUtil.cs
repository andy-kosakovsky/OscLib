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
    /// Internal utility class for various helper methods.
    /// </summary>
    internal static class OscUtil
    {
        /// <summary> Returns an end point at a local address on a random open port. </summary> 
        internal static IPEndPoint GetLocalEndPoint()
        { 
            return new IPEndPoint(OscProtocol.LocalIP, 0); 
        } 


        internal static IPEndPoint GetLocalEndPointWithPort(int port)
        {
            return new IPEndPoint(OscProtocol.LocalIP, port);
        }

        /// <summary>
        /// Prints array of bytes in hex form as a formatted string sequence.
        /// </summary>
        /// <param name="array"> Source array of bytes. </param>
        /// <param name="bytesPerLine"> How many bytes will be printed in a line. </param>
        /// <returns> A formatted sequence of strings. </returns>
        internal static string ByteArrayToStrings(byte[] array, int bytesPerLine)
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
        internal static int GetNextMultipleOfFour(int number)
        {
            return ((number / 4) + 1) * 4;
        }

        
        /// <summary>
        /// Swaps the endianness of the provided array of bytes.
        /// </summary>
        /// <param name="data"> Array of bytes to be swapped (needs to be of an even length to work). </param>
        internal static void SwapEndian(byte[] data)
        {
            for (int i = 0; i < data.Length / 2; i++)
            {
                byte currentValue = data[i];
                data[i] = data[data.Length - 1 - i];
                data[data.Length - 1 - i] = currentValue;
            }

        }

        internal static bool IsNumberBetween(int check, int boundOne, int boundTwo)
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
