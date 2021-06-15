using System;
using System.Text;

namespace OscLib
{
    /// <summary>
    /// Contains methods for deserializing OSC binary data.
    /// </summary>
    public static class OscDeserializer
    {
        #region INT32
        /// <summary>
        /// Converts four bytes of OSC binary data - beginning at the specified index in the provided array - into a 32-bit integer. 
        /// </summary>
        /// <remarks> OSC being a big-endian protocol, this method expects big-endian binary data. </remarks>
        /// <param name="data"> Byte array containing OSC binary data. </param>
        /// <param name="pointer"> Points at the index of the first relevant byte. </param>
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
        /// Converts four bytes of OSC binary data - beginning at the specified index in the provided array - into a 32-bit integer. 
        /// The pointer is a reference, its value is increased according to the size of added data.
        /// </summary>
        /// <remarks> OSC being a big-endian protocol, this method expects big-endian binary data. </remarks>
        /// <param name="data"> Byte array containing OSC binary data. </param>
        /// <param name="extPointer"> Points at the index of the first relevant byte. </param>
        public static int GetInt32(byte[] data, ref int extPointer)
        {
            int value = GetInt32(data, extPointer);

            // move pointer
            extPointer += OscProtocol.Chunk32;

            return value;
        }

        #endregion // INT32


        #region INT64
        /// <summary>
        /// Converts eight bytes of OSC binary data - beginning at the specified index in the provided array - into a 64-bit integer. 
        /// </summary>
        /// <remarks> OSC being a big-endian protocol, this method expects big-endian binary data. </remarks>
        /// <param name="data"> Byte array containing OSC binary data. </param>
        /// <param name="pointer"> Points at the index of the first relevant byte. </param>
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
        /// Converts eight bytes of OSC binary data - beginning at the specified index in the provided array - into a 64-bit integer. 
        /// The pointer is a reference, its value is increased according to the size of added data.
        /// </summary>
        /// <remarks> OSC being a big-endian protocol, this method expects big-endian binary data. </remarks>
        /// <param name="data"> Byte array containing OSC binary data. </param>
        /// <param name="extPointer"> Points at the index of the first relevant byte. </param>
        public static long GetInt64(byte[] data, ref int extPointer)
        {
            long value = GetInt64(data, extPointer);

            extPointer += OscProtocol.Chunk64;

            return value;
        }

        #endregion // INT64


        #region TIMETAG
        /// <summary>
        /// Converts eight bytes of OSC binary data - beginning at the specified index in the provided array - into an OSC Timetag. 
        /// </summary>
        /// <remarks> OSC being a big-endian protocol, this method expects big-endian binary data. </remarks>
        /// <param name="data"> Byte array containing OSC binary data. </param>
        /// <param name="pointer"> Points at the index of the first relevant byte. </param>
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
        /// Converts eight bytes of OSC binary data - beginning at the specified index in the provided array - into an OSC Timetag.
        /// The pointer is a reference, its value is increased according to the size of added data.
        /// </summary>
        /// <remarks> OSC being a big-endian protocol, this method expects big-endian binary data. </remarks>
        /// <param name="data"> Byte array containing OSC binary data. </param>
        /// <param name="extPointer"> Points at the index of the first relevant byte. </param>
        public static OscTimetag GetTimetag(byte[] data, ref int extPointer)
        {
            OscTimetag timetag = GetTimetag(data, extPointer);

            extPointer += OscProtocol.Chunk64;

            return timetag;
        }

        #endregion // TIMETAG


        #region FLOAT32
        /// <summary>
        /// Converts four bytes of OSC binary data - beginning at the specified index in the provided array - into a 32-bit (single-presicion) floating-point number. 
        /// </summary>
        /// <remarks> OSC being a big-endian protocol, this method expects big-endian binary data. </remarks>
        /// <param name="data"> Byte array containing OSC binary data. </param>
        /// <param name="pointer"> Points at the index of the first relevant byte. </param>
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
        /// Converts four bytes of OSC binary data - beginning at the specified index in the provided array - into a 32-bit (single-presicion) floating-point number.  
        /// The pointer is a reference, its value is increased according to the size of added data.
        /// </summary>
        /// <remarks> OSC being a big-endian protocol, this method expects big-endian binary data. </remarks>
        /// <param name="data"> Byte array containing OSC binary data. </param>
        /// <param name="extPointer"> Points at the index of the first relevant byte. </param>
        public static float GetFloat32(byte[] data, ref int extPointer)
        {
            float value = GetFloat32(data, extPointer);

            extPointer += OscProtocol.Chunk32;

            return value;
        }

        #endregion // FLOAT32


        #region FLOAT64
        /// <summary>
        /// Converts four bytes of OSC binary data - beginning at the specified index in the provided array - into a 64-bit (double-presicion) floating-point number. 
        /// </summary>
        /// <remarks> OSC being a big-endian protocol, this method expects big-endian binary data. </remarks>
        /// <param name="data"> Byte array containing OSC binary data. </param>
        /// <param name="pointer"> Points at the index of the first relevant byte. </param>
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
        /// Converts four bytes of OSC binary data - beginning at the specified index in the provided array - into a 64-bit (double-presicion) floating-point number.  
        /// The pointer is a reference, its value is increased according to the size of added data.
        /// </summary>
        /// <remarks> OSC being a big-endian protocol, this method expects big-endian binary data. </remarks>
        /// <param name="data"> Byte array containing OSC binary data. </param>
        /// <param name="extPointer"> Points at the index of the first relevant byte. </param>
        public static double GetFloat64(byte[] data, ref int extPointer)
        {
            double value = GetFloat64(data, extPointer);

            extPointer += OscProtocol.Chunk64;

            return value;
        }

        #endregion // FLOAT64


        #region BLOB
        /// <summary>
        /// Retreives a binary blob of OSC data from a larger set, as per OSC specification. Returns it as a separate byte array.
        /// </summary>
        /// <remarks> This method is chiefly intended for retreiving "blob" arguments from OSC Messages. </remarks>
        /// <param name="data"> Byte array containing OSC binary data. </param>
        /// <param name="pointer"> Points at the index of the first relevant byte. </param>
        public static byte[] GetBlob(byte[] data, int pointer)
        {          
            // get length
            int length = GetInt32(data, pointer);

            byte[] resultArray = new byte[length];

            // get result
            Array.Copy(data, pointer + OscProtocol.Chunk32, resultArray, 0, length);

            return resultArray;
        }


        /// <summary>
        /// Retreives a binary blob of OSC data from a larger set, as per OSC specification. Returns it as a separate byte array.
        /// The pointer is a reference, its value is increased according to the size of added data.
        /// </summary>
        /// <remarks> This method is chiefly intended for retreiving "blob" arguments from OSC Messages. </remarks>
        /// <param name="data"> Byte array containing OSC binary data. </param>
        /// <param name="extPointer"> Points at the index of the first relevant byte. </param>
        public static byte[] GetBlob(byte[] data, ref int extPointer)
        {
            // get array
            byte[] resultArray = GetBlob(data, extPointer);

            // shift the pointer 
            extPointer += OscSerializer.GetOscLength(resultArray);

            return resultArray;
        }

        #endregion // BLOB


        #region STRING
        /// <summary>
        /// Converts a sequence of OSC binary data - beginning at the specified index in the provided array - into a string, as per OSC specification. 
        /// </summary>
        /// <remarks> Expects a sequence of ASCII bytes, null-terminated. </remarks>
        /// <param name="data"> Byte array containing OSC binary data. </param>
        /// <param name="pointer"> Points at the index of the first relevant byte. </param>
        public static string GetString(byte[] data, int pointer)
        {
            int index = pointer;
            return GetString(data, ref index);
        }


        /// <summary>
        /// Converts a sequence of OSC binary data - beginning at the specified index in the provided array - into a string, as per OSC specification. 
        /// The pointer is a reference, its value is increased according to the size of added data.
        /// </summary>
        /// <remarks> Expects a sequence of ASCII bytes, null-terminated. </remarks>
        /// <param name="data"> Byte array containing OSC binary data. </param>
        /// <param name="extPointer"> Points at the index of the first relevant byte. </param>
        public static string GetString(byte[] data, ref int extPointer)
        {
            int start = extPointer;
            int count = 0;

            // scan chunks until we hit some nulls, then get to a multiple of 4 and stop
            while (data[extPointer] != 0)
            {
                extPointer++;
                count++;
            }

            extPointer = extPointer.NextX4();

            return Encoding.ASCII.GetString(data, start, count);
        }

        #endregion // STRING


        #region OSC STRING
        /// <summary>
        /// Converts a sequence of OSC binary data - beginning at the specified index in the provided array - into a string, as per OSC specification.
        /// Returns it as an OscString struct.
        /// </summary>
        /// <remarks> Expects a sequence of ASCII bytes, null-terminated. </remarks>
        /// <param name="data"> Byte array containing OSC binary data. </param>
        /// <param name="pointer"> Points at the index of the first relevant byte. </param>
        public static OscString GetOscString(byte[] data, int pointer)
        {
            int index = pointer;
            return GetOscString(data, ref index);
        }


        /// <summary>
        /// Converts a sequence of OSC binary data - beginning at the specified index in the provided array - into a string, as per OSC specification.
        /// Returns it as an OscString struct.
        /// The pointer is a reference, its value is increased according to the size of added data.
        /// </summary>
        /// <remarks> Expects a sequence of ASCII bytes, null-terminated. </remarks>
        /// <param name="data"> Byte array containing OSC binary data. </param>
        /// <param name="pointer"> Points at the index of the first relevant byte. </param>
        public static OscString GetOscString(byte[] data, ref int pointer)
        {
            int start = pointer;
            int count = 0;

            while (data[pointer] != 0)
            {
                pointer++;
                count++;
            }

            pointer = pointer.NextX4();

            return new OscString(data, start, count);

        }

        #endregion // OSC STRING


        #region OSC MIDI
        /// <summary>
        /// Converts four bytes of OSC binary data - beginning at the specified index in the provided array - into a MIDI message, represented by the OscMidi struct.
        /// </summary>
        /// <param name="data"> Byte array containing OSC binary data. </param>
        /// <param name="pointer"> Points at the index of the first relevant byte. </param>
        public static OscMidi GetOscMidi(byte[] data, int pointer)
        {
            return new OscMidi(data[pointer],
                               data[pointer + 1],
                               data[pointer + 2],
                               data[pointer + 3]);
        }


        /// <summary>
        /// Converts four bytes of OSC binary data - beginning at the specified index in the provided array - into a MIDI message, represented by the OscMidi struct.
        /// The pointer is a reference, its value is increased according to the size of added data.
        /// </summary>
        /// <param name="data"> Byte array containing OSC binary data. </param>
        /// <param name="pointer"> Points at the index of the first relevant byte. </param>
        public static OscMidi GetOscMidi(byte[] data, ref int pointer)
        {
            pointer += OscProtocol.Chunk32;
            return GetOscMidi(data, pointer - OscProtocol.Chunk32);
        }

        #endregion // OSC MIDI


        #region OSC COLOR
        /// <summary>
        /// Converts four bytes of OSC binary data - beginning at the specified index in the provided array - into an 32-bit RGBA color, represented by the OscColor struct.
        /// </summary>
        /// <param name="data"> Byte array containing OSC binary data. </param>
        /// <param name="pointer"> Points at the index of the first relevant byte. </param>
        public static OscColor GetOscColor(byte[] data, int pointer)
        {
            return new OscColor(data[pointer],
                               data[pointer + 1],
                               data[pointer + 2],
                               data[pointer + 3]);
        }


        /// <summary>
        /// Converts four bytes of OSC binary data - beginning at the specified index in the provided array - into an 32-bit RGBA color, represented by the OscColor struct.
        /// The pointer is a reference, its value is increased according to the size of added data.
        /// </summary>
        /// <param name="data"> Byte array containing OSC binary data. </param>
        /// <param name="pointer"> Points at the index of the first relevant byte. </param>
        public static OscColor GetOscColor(byte[] data, ref int pointer)
        {
            pointer += OscProtocol.Chunk32;
            return GetOscColor(data, pointer - OscProtocol.Chunk32);
        }

        #endregion // OSC COLOR

    }

}