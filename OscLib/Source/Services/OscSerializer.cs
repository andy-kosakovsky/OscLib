using System;
using System.Collections.Generic;
using System.Text;

namespace OscLib
{
    /// <summary>
    /// Serializes data into bytes. Makes sure it is big-endian where appropriate.
    /// </summary>
    public static class OscSerializer
    {

        #region INT32

        /// <summary>
        /// Converts an integer into a byte array, swapping its endianness if needed.
        /// </summary>
        /// <param name="arg"> An integer to be converted. </param>
        /// <returns> A byte array. </returns>
        public static byte[] GetBytes(int arg)
        {
            int value = arg;

            if (BitConverter.IsLittleEndian)
            {
                value = OscEndian.Swap(value);
            }

            return BitConverter.GetBytes(value);
        }


        /// <summary>
        /// Converts an integer into bytes and adds them to an existing array, swapping their endianness if needed and shifting the pointer accordingly.
        /// </summary>
        /// <param name="arg"> An integer to be converted. </param>
        /// <param name="array"> The target byte array. </param>
        /// <param name="extPointer">The index from which to add data. Will be shifted forwards by the length of added data. </param>
        public static void AddBytes(int arg, byte[] array, ref int extPointer)
        {
            int value = arg;

            if (BitConverter.IsLittleEndian)
            {
                value = OscEndian.Swap(value);
            }

            BitConverter.GetBytes(value).CopyTo(array, extPointer);

            // shift the external pointer
            extPointer += OscProtocol.Chunk32;
        }


        /// <summary>
        /// Converts an integer into bytes and adds them to an existing array, swapping their endianness if needed.
        /// </summary>
        /// <param name="arg"> An integer to be converted. </param>
        /// <param name="array"> The target byte array. </param>
        /// <param name="pointer">The index from which to add data. </param>
        public static void AddBytes(int arg, byte[] array, int pointer)
        {
            int value = arg;

            if (BitConverter.IsLittleEndian)
            {
                value = OscEndian.Swap(value);
            }

            BitConverter.GetBytes(value).CopyTo(array, pointer);
        }

        #endregion // INT32



        #region INT64

        /// <summary>
        /// Converts a longint into a byte array, swapping its endianness if needed.
        /// </summary>
        /// <param name="arg"> A longint to be converted. </param>
        /// <returns> A byte array. </returns>
        public static byte[] GetBytes(long arg)
        {
            long value = arg;

            if (BitConverter.IsLittleEndian)
            {
                value = OscEndian.Swap(value);
            }

            return BitConverter.GetBytes(value);
        }


        /// <summary>
        /// Converts a longint into bytes and adds them to an existing array, swapping their endianness if needed and shifting the pointer accordingly.
        /// </summary>
        /// <param name="arg"> A longint to be converted. </param>
        /// <param name="array"> The target byte array. </param>
        /// <param name="extPointer"> The index from which to add data. Will be shifted forwards by the length of added data.  </param>
        public static void AddBytes(long arg, byte[] array, ref int extPointer)
        {
            long value = arg;

            if (BitConverter.IsLittleEndian)
            {
                value = OscEndian.Swap(value);
            }

            BitConverter.GetBytes(value).CopyTo(array, extPointer);

            // shift the external pointer
            extPointer += OscProtocol.Chunk64;
        }


        /// <summary>
        /// Converts a longint into bytes and adds them to an existing array, swapping their endianness if needed.
        /// </summary>
        /// <param name="arg"> A longint to be converted. </param>
        /// <param name="array"> The target byte array. </param>
        /// <param name="pointer"> The index from which to add data. </param>
        public static void AddBytes(long arg, byte[] array, int pointer)
        {
            long value = arg;

            if (BitConverter.IsLittleEndian)
            {
                value = OscEndian.Swap(value);
            }

            BitConverter.GetBytes(value).CopyTo(array, pointer);
        }

        #endregion // INT64



        #region FLOAT32

        /// <summary>
        /// Converts a float into a byte array, swapping its endianness if needed.
        /// </summary>
        /// <param name="arg"> The float to be converted. </param>
        /// <returns> A byte array. </returns>
        public static byte[] GetBytes(float arg)
        {
            float value = arg;

            if (BitConverter.IsLittleEndian)
            {
                value = OscEndian.Swap(value);
            }

            return BitConverter.GetBytes(value);
        }


        /// <summary>
        /// Converts a float into bytes and adds them to an existing array, swapping their endianness if needed and shifting the pointer accordingly.
        /// </summary>
        /// <param name="arg"> The float to be converted. </param>
        /// <param name="array"> The target byte array. </param>
        /// <param name="extPointer">The index from which to add data. Will be shifted forwards by the length of added data. </param>
        public static void AddBytes(float arg, byte[] array, ref int extPointer)
        {
            float value = arg;

            if (BitConverter.IsLittleEndian)
            {
                value = OscEndian.Swap(value);
            }

            BitConverter.GetBytes(value).CopyTo(array, extPointer);

            // shift the external pointer
            extPointer += OscProtocol.Chunk32;
        }


        /// <summary>
        /// Converts a float into bytes and adds them to an existing array, swapping their endianness if needed.
        /// </summary>
        /// <param name="arg"> The float to be converted. </param>
        /// <param name="array"> The target byte array. </param>
        /// <param name="pointer">The index from which to add data. </param>
        public static void AddBytes(float arg, byte[] array, int pointer)
        {
            float value = arg;

            if (BitConverter.IsLittleEndian)
            {
                value = OscEndian.Swap(value);
            }

            BitConverter.GetBytes(value).CopyTo(array, pointer);
        }

        #endregion // FLOAT32



        #region FLOAT64

        /// <summary>
        /// Converts a double into a byte array, swapping its endianness if needed.
        /// </summary>
        /// <param name="arg"> The double to be converted. </param>
        /// <returns> A byte array. </returns>
        public static byte[] GetBytes(double arg)
        {
            double value = arg;

            if (BitConverter.IsLittleEndian)
            {
                value = OscEndian.Swap(value);
            }

            return BitConverter.GetBytes(value);
        }


        /// <summary>
        /// Converts a double into bytes and adds them to an existing array, swapping their endianness if needed.
        /// </summary>
        /// <param name="arg"> The double to be converted. </param>
        /// <param name="array"> The target byte array. </param>
        /// <param name="extPointer">The index from which to add data. Will be shifted forwards by the length of added data. </param>
        public static void AddBytes(double arg, byte[] array, ref int extPointer)
        {
            double value = arg;

            if (BitConverter.IsLittleEndian)
            {
                value = OscEndian.Swap(value);
            }

            BitConverter.GetBytes(value).CopyTo(array, extPointer);

            // shift the external pointer
            extPointer += OscProtocol.Chunk64;
        }


        /// <summary>
        /// Converts a double into bytes and adds them to an existing array, swapping their endianness if needed.
        /// </summary>
        /// <param name="arg"> The double to be converted. </param>
        /// <param name="array"> The target byte array. </param>
        /// <param name="pointer">The index from which to add data. Will be shifted forwards by the length of added data. </param>
        public static void AddBytes(double arg, byte[] array, int pointer)
        {
            double value = arg;

            if (BitConverter.IsLittleEndian)
            {
                value = OscEndian.Swap(value);
            }

            BitConverter.GetBytes(value).CopyTo(array, pointer);
        }

        #endregion  // FLOAT64



        #region STRING

        /// <summary>
        /// Converts a string into an ASCII byte array.
        /// </summary>
        /// <param name="arg"> The string to be converted. </param>
        /// <returns> A byte array. </returns>
        public static byte[] GetBytes(string arg)
        {
            // just to simplify conversion. will create two small byte[] arrays but who cares lol
            OscString oscString = arg;

            return oscString.GetOscBytes();
        }


        /// <summary>
        /// Converts a string into ASCII bytes and adds them to an existing byte array.
        /// </summary>
        /// <param name="arg"> The string to be converted. </param>
        /// <param name="array"> The target byte array. </param>
        /// <param name="extPointer">The index from which to add data. Will be shifted forwards by the length of added data. </param>
        public static void AddBytes(string arg, byte[] array, ref int extPointer)
        {
            // this shouldn't actually create more than one byte array
            OscString oscString = arg;

            oscString.CopyTo(array, extPointer);

            // shift the external pointer
            extPointer += oscString.OscLength;
        }


        /// <summary>
        /// Converts a string into ASCII bytes and adds them to an existing byte array.
        /// </summary>
        /// <param name="arg"> The string to be converted. </param>
        /// <param name="array"> The target byte array. </param>
        /// <param name="pointer">The index from which to add data. Will be shifted forwards by the length of added data. </param>
        public static void AddBytes(string arg, byte[] array, int pointer)
        {
            // this shouldn't actually create more than one byte array
            OscString oscString = arg;

            oscString.CopyTo(array, pointer);
        }


        /// <summary>
        /// Returns the OSC protocol-compliant byte length of the string.
        /// </summary>
        /// <param name="arg"> String to be measured. </param>
        /// <returns> OSC length of the string. </returns>
        public static int GetLength(string arg)
        {
            return OscUtil.GetNextMultipleOfFour(arg.Length);
        }

        #endregion



        #region OSC STRING

        /// <summary>
        /// Converts an OSC String into an ASCII byte array.
        /// </summary>
        /// <param name="arg"> String to be converted. </param>
        /// <returns> A byte array. </returns>
        public static byte[] GetBytes(OscString arg)
        {
            return arg.GetOscBytes();
        }


        /// <summary>
        /// Converts an OSC String into ASCII bytes and adds them to an existing byte array.
        /// </summary>
        /// <param name="arg"> The string to be converted. </param>
        /// <param name="array"> The target byte array. </param>
        /// <param name="extPointer">The index from which to add data. Will be shifted forwards by the length of added data. </param>
        public static void AddBytes(OscString arg, byte[] array, ref int extPointer)
        {
            arg.CopyTo(array, extPointer);

            extPointer += arg.OscLength;
        }


        /// <summary>
        /// Converts an OSC String into ASCII bytes and adds them to an existing byte array.
        /// </summary>
        /// <param name="arg"> The string to be converted. </param>
        /// <param name="array"> The target byte array. </param>
        /// <param name="pointer">The index from which to add data. Will be shifted forwards by the length of added data. </param>
        public static void AddBytes(OscString arg, byte[] array, int pointer)
        {
            // for consistency's sake
            arg.CopyTo(array, pointer);
        }


        /// <summary>
        /// Returns the OSC protocol-compliant byte length of the string.
        /// </summary>
        /// <param name="arg"> String to be measured. </param>
        /// <returns> OSC length of the string. </returns>
        public static int GetLength(OscString arg)
        {
            return arg.OscLength;
        }

        #endregion



        #region BLOB

        /// <summary>
        /// Formats a byte array into an OSC Protocol-compliant binary blob and returns it as a copy.
        /// </summary>
        /// <param name="arg"> Byte array to be converted. </param>
        /// <returns> A binary blob - still a byte array but correctly formatted. </returns>
        public static byte[] GetBytes(byte[] arg)
        {
            byte[] resultArray = new byte[GetLength(arg) + OscProtocol.Chunk32];

            int pointer = 0;

            AddBytes(arg, resultArray, ref pointer);

            return resultArray;
        }


        /// <summary>
        /// Formats a byte array to be OSC Protocol-compliant and adds it to an existing byte array.
        /// </summary>
        /// <param name="arg"> The byte array to be formatted. </param>
        /// <param name="array"> The target byte array. </param>
        /// <param name="extPointer">The index from which to add data. Will be shifted forwards by the length of added data. </param>
        public static void AddBytes(byte[] arg, byte[] array, ref int extPointer)
        {
            // TODO: adding blobs needs testing
            // add length
            AddBytes(arg.Length, array, ref extPointer);
            // add data
            arg.CopyTo(array, extPointer);

            extPointer += GetLength(arg); 
        }


        /// <summary>
        /// Formats a byte array to be OSC Protocol-compliant and adds it to an existing byte array.
        /// </summary>
        /// <param name="arg"> The byte array to be formatted. </param>
        /// <param name="array"> The target byte array. </param>
        /// <param name="pointer">The index from which to add data. Will be shifted forwards by the length of added data. </param>
        public static void AddBytes(byte[] arg, byte[] array, int pointer)
        {
            // TODO: adding blobs needs testing
            // add length
            AddBytes(arg.Length, array, pointer);
            // add data
            arg.CopyTo(array, pointer);
        }


        /// <summary>
        /// Returns the OSC Protocol-compliant length of the byte array.
        /// </summary>
        /// <param name="arg"> Byte array to be measured. </param>
        /// <returns> OSC length of the array. </returns>
        public static int GetLength(byte[] arg)
        {
            return OscUtil.GetNextMultipleOfFour(arg.Length);
        }

        #endregion



        #region TIMETAG

        /// <summary>
        /// Converts an OSC Timetag into a byte array.
        /// </summary>
        /// <param name="arg"> The timetag to be converted. </param>
        /// <returns> A byte array. </returns>
        public static byte[] GetBytes(OscTimetag arg)
        {
            ulong value = arg.NtpTimestamp;
            
            if (BitConverter.IsLittleEndian)
            {
                value = OscEndian.Swap(value);
            }

            return BitConverter.GetBytes(value);
        }


        /// <summary>
        /// Converts an OSC Timetag into bytes and adds them to an existing array. Shifts the pointer forward accordingly.
        /// </summary>
        /// <param name="arg"> The timetag to be converted. </param>
        /// <param name="array"> The target byte array. </param>
        /// <param name="extPointer"> The index from which to add data. Will be shifted forwards by the length of added data. </param>
        public static void AddBytes(OscTimetag arg, byte[] array, ref int extPointer)
        {
            AddBytes(arg, array, extPointer);

            // shift the external pointer
            extPointer += OscProtocol.Chunk64;
        }


        /// <summary>
        /// Converts an OSC Timetag into bytes and adds them to an existing array.
        /// </summary>
        /// <param name="arg"> The timetag to be converted. </param>
        /// <param name="array"> The target byte array. </param>
        /// <param name="pointer">The index from which to add data. </param>
        public static void AddBytes(OscTimetag arg, byte[] array, int pointer)
        {
            ulong value = arg.NtpTimestamp;

            if (BitConverter.IsLittleEndian)
            {
                value = OscEndian.Swap(value);
            }

            BitConverter.GetBytes(value).CopyTo(array, pointer);
        }


        #endregion // TIMETAG



        #region COLOR

        public static byte[] GetBytes(OscColor color)
        {
            byte[] result = new byte[4];

            result[0] = color.Red;
            result[1] = color.Green;
            result[2] = color.Blue;
            result[3] = color.Alpha;

            return result;
        }


        public static void AddBytes(OscColor color, byte[] array, ref int extPointer)
        {
            array[extPointer++] = color.Red;
            array[extPointer++] = color.Green;
            array[extPointer++] = color.Blue;
            array[extPointer++] = color.Alpha;
        }


        public static void AddBytes(OscColor color, byte[] array, int pointer)
        {
            int index = pointer;
            AddBytes(color, array, ref index);
        }

        #endregion // COLOR


        #region MIDI

        public static byte[] GetBytes(OscMidi midi)
        {
            byte[] result = new byte[4];

            result[0] = midi.PortId;
            result[1] = midi.Status;
            result[2] = midi.Data1;
            result[3] = midi.Data2;

            return result;
        }


        public static void AddBytes(OscMidi midi, byte[] array, ref int extPointer)
        {
            array[extPointer++] = midi.PortId;
            array[extPointer++] = midi.Status;
            array[extPointer++] = midi.Data1;
            array[extPointer++] = midi.Data2;
        }


        public static void AddBytes(OscMidi midi, byte[] array, int pointer)
        {
            int index = pointer;
            AddBytes(midi, array, ref index);
        }

        #endregion // MIDI




    }

}
