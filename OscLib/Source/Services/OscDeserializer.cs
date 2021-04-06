using System;
using System.Text;
using System.Collections.Generic;


namespace OscLib
{

    /// <summary>
    /// Deserializes OSC binary data, translating it into readable messages and bundles.
    /// </summary>
    public static class OscDeserializer
    {


        #region GET ARGUMENTS (WITH EXTERNAL POINTER)
        

        /// <summary>
        /// Gets an int out of the byte array, using an external pointer.
        /// </summary>
        /// <param name="data"> Byte array containing the data. </param>
        /// <param name="pointer"> Pointing at the index from which the relevant bytes begin. </param>
        public static int GetInt32(byte[] data, ref int pointer)
        {
            int value = GetInt32(data, pointer);

            // move pointer
            pointer += OscProtocol.Chunk32;

            return value;
        }


        /// <summary>
        /// Gets a longint out of the byte array, using an external pointer.
        /// </summary>
        /// <param name="data"> Byte array containing the data. </param>
        /// <param name="pointer"> Pointing at the index from which the relevant bytes begin. </param>
        public static long GetInt64(byte[] data, ref int pointer)
        {
            long value = GetInt64(data, pointer);

            pointer += OscProtocol.Chunk64;

            return value;
        }


        /// <summary>
        /// Gets a timestamp out of the byte array, using an external pointer.
        /// </summary>
        /// <param name="pointer"> Pointing at the index from which the relevant bytes begin. </param>
        /// <param name="data"> Byte array containing the data. </param>
        /// <returns> An OscTimestamp. </returns>
        public static OscTimetag GetTimetag(byte[] data, ref int pointer)
        {
            OscTimetag timetag = GetTimetag(data, pointer);

            pointer += OscProtocol.Chunk64;

            return timetag;
        }


        /// <summary>
        /// Gets a float out of the byte array, using an external pointer.
        /// </summary>
        /// <param name="data"> Byte array containing the data. </param>
        /// <param name="pointer"> Pointing at the index from which the relevant bytes begin. </param>
        public static float GetFloat32(byte[] data, ref int pointer)
        {
            float value = GetFloat32(data, pointer);

            pointer += OscProtocol.Chunk32;

            return value;
        }


        /// <summary>
        /// Gets a double out of the byte array, using an external pointer.
        /// </summary>
        /// <param name="data"> Byte array containing the data. </param>
        /// <param name="pointer"> Pointing at the index from which the relevant bytes begin. </param>
        public static double GetFloat64(byte[] data, ref int pointer)
        {
            double value = GetFloat64(data, pointer);

            pointer += OscProtocol.Chunk64;

            return value;
        }


        /// <summary>
        /// Gets a binary blob out of the byte array, using an external pointer.
        /// </summary>
        /// <param name="data"> Byte array containing the data. </param>
        /// <param name="pointer"> Pointing at the index from which the relevant bytes begin. </param>
        /// <returns> A byte array. </returns>
        public static byte[] GetBlob(byte[] data, ref int pointer)
        {
            // get length
            int length = GetInt32(data, ref pointer);

            byte[] resultArray = new byte[length];

            // get result
            Array.Copy(data, pointer, resultArray, 0, length);

            // shift pointer (by length + a few empty bytes at the end)
            pointer += OscUtil.GetNextMultipleOfFour(length);

            return resultArray;
        }


        /// <summary>
        /// Gets a string out of the byte array, using an external pointer.
        /// </summary>
        /// <param name="data"> Byte array containing the data. </param>
        /// <param name="pointer"> Pointing at the index from which the relevant bytes begin. </param>
        public static string GetString(byte[] data, ref int pointer)
        {
            StringBuilder returnString = new StringBuilder();

            // scan chunks until we hit some nulls, then get to a multiple of 4 and stop
            while (data[pointer] != 0)
            {
                returnString.Append((char)data[pointer]);
                pointer++;
            }

            pointer = OscUtil.GetNextMultipleOfFour(pointer);

            return returnString.ToString();
        }

        public static OscString GetOscString(byte[] data, ref int pointer)
        {
            throw new NotImplementedException();
        }



        public static OscString GetOscMidi(byte[] data, ref int pointer)
        {
            throw new NotImplementedException();
        }

        #endregion



        #region GET ARGUMENTS (WITH DIRECT POINTER)

        /// <summary>
        /// Gets an int out of the byte array, using a pointer.
        /// </summary>
        /// <param name="data"> Byte array containing the data. </param>
        /// <param name="pointer"> Pointing at the index from which the relevant bytes begin. </param>
        public static int GetInt32(byte[] data, int pointer)
        {
            int value = BitConverter.ToInt32(data, pointer);

            // swap endianness if needed
            if (BitConverter.IsLittleEndian)
            {
                value = OscEndian.Swap(value);
            }

            return value;
        }


        /// <summary>
        /// Gets a long out of the byte array, using a pointer.
        /// </summary>
        /// <param name="data"> Byte array containing the data. </param>
        /// <param name="pointer"> Pointing at the index from which the relevant bytes begin. </param>
        public static long GetInt64(byte[] data, int pointer)
        {
            long value = BitConverter.ToInt64(data, pointer);

            if (BitConverter.IsLittleEndian)
            {
                value = OscEndian.Swap(value);
            }

            return value;
        }


        /// <summary>
        /// Gets a timestamp out of the byte array, using a pointer.
        /// </summary>
        /// <param name="data"> Byte array containing the data. </param>
        /// <param name="pointer"> Pointing at the index from which the relevant bytes begin. </param>
        public static OscTimetag GetTimetag(byte[] data, int pointer)
        {
            ulong ntpTimestamp = BitConverter.ToUInt64(data, pointer);

            if (BitConverter.IsLittleEndian)
            {
                ntpTimestamp = OscEndian.Swap(ntpTimestamp);
            }

            return new OscTimetag(ntpTimestamp);
        }


        /// <summary>
        /// Gets a float out of the byte array, using a pointer.
        /// </summary>
        /// <param name="data"> Byte array containing the data. </param>
        /// <param name="pointer"> Pointing at the index from which the relevant bytes begin. </param>
        public static float GetFloat32(byte[] data, int pointer)
        {
            float value = BitConverter.ToSingle(data, pointer);

            if (BitConverter.IsLittleEndian)
            {
                value = OscEndian.Swap(value);
            }

            return value;
        }


        /// <summary>
        /// Gets a double out of the byte array, using a pointer.
        /// </summary>
        /// <param name="data"> Byte array containing the data. </param>
        /// <param name="pointer"> Pointing at the index from which the relevant bytes begin. </param>
        public static double GetFloat64(byte[] data, int pointer)
        {
            double value = BitConverter.ToDouble(data, pointer);

            if (BitConverter.IsLittleEndian)
            {
                value = OscEndian.Swap(value);
            }

            return value;
        }


        /// <summary>
        /// Gets a binary blob out of the byte array, using a pointer.
        /// </summary>
        /// <param name="data"> Byte array containing the data. </param>
        /// <param name="pointer"> Pointing at the index from which the relevant bytes begin. </param>
        /// <returns> A byte array. </returns>
        public static byte[] GetBlob(byte[] data, int pointer)
        {
            int index = pointer;

            // get length
            int length = GetInt32(data, ref index);

            byte[] resultArray = new byte[length];

            // get result
            Array.Copy(data, index, resultArray, 0, length);

            return resultArray;
        }


        /// <summary>
        /// Gets a string out of the byte array, using a pointer.
        /// </summary>
        /// <param name="data"> Byte array containing the data. </param>
        /// <param name="pointer"> Pointing at the index from which the relevant bytes begin. </param>
        public static string GetString(byte[] data, int pointer)
        {
            int index = pointer;
            StringBuilder returnString = new StringBuilder();

            // scan bytes until we hit a 0, then just stop.
            while (data[index] != 0)
            {
                returnString.Append((char)data[pointer]);
                index++;
            }

            return returnString.ToString();
        }


        public static OscString GetOscString(byte[] data, int pointer)
        {
            throw new NotImplementedException();
        }


        
        public static OscString GetOscMidi(byte[] data, int pointer)
        {
            throw new NotImplementedException();
        }

        #endregion

    }

}




