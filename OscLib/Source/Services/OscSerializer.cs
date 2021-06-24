using System;

namespace OscLib
{
    /// <summary>
    /// Contains methods for serializing various data types into OSC Protocol-compliant binary data.
    /// </summary>
    public static class OscSerializer
    {
        #region INT32
        /// <summary>
        /// Returns the specified 32-bit integer value as a big-endian sequence of bytes.
        /// </summary>
        /// <param name="arg"> The value to convert. </param>
        /// <returns> An array of four bytes. </returns>
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
        /// Converts the specified 32-bit integer value into a big-endian sequence of bytes, adds it to the provided byte array at the specified index.
        /// The pointer is passed by reference, its value is increased according to the size of added data.
        /// </summary>
        /// <param name="arg"> The value to convert. </param>
        /// <param name="array"> A byte array (presumably containing OSC binary data) to which the sequence of bytes will be added. </param>
        /// <param name="extPointer"> Points at the destination index for the byte sequence. </param>
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
        /// Converts the specified 32-bit integer value into a big-endian sequence of bytes, adds it to the provided byte array at the specified index.
        /// </summary>
        /// <param name="arg"> The value to convert. </param>
        /// <param name="array"> A byte array (presumably containing OSC binary data) to which the sequence of bytes will be added. </param>
        /// <param name="pointer"> Points at the destination index for the byte sequence. </param>
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
        /// Returns the specified 64-bit integer value as a big-endian sequence of bytes.
        /// </summary>
        /// <param name="arg"> The value to convert. </param>
        /// <returns> An array of eight bytes. </returns>
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
        /// Converts the specified 64-bit integer into a big-endian sequence of bytes, adds it to the provided byte array at the specified index.
        /// The pointer is passed by reference, its value is increased according to the size of added data.
        /// </summary>
        /// <param name="arg"> The value to convert. </param>
        /// <param name="array"> A byte array (presumably containing OSC binary data) to which the sequence of bytes will be added. </param>
        /// <param name="extPointer"> Points at the destination index for the byte sequence. </param>
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
        /// Converts the specified 64-bit integer into a big-endian sequence of bytes, adds it to the provided byte array at the specified index.
        /// </summary>
        /// <param name="arg"> The value to convert. </param>
        /// <param name="array"> A byte array (presumably containing OSC binary data) to which the sequence of bytes will be added. </param>
        /// <param name="pointer"> Points at the destination index for the byte sequence. </param>
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
        /// Returns the specified 32-bit float value as a big-endian sequence of bytes.
        /// </summary>
        /// <param name="arg"> The value to convert. </param>
        /// <returns> An array of four bytes. </returns>
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
        /// Converts the specified 32-bit float value into a big-endian sequence of bytes, adds it to the provided byte array at the specified index.
        /// The pointer is passed by reference, its value is increased according to the size of added data.
        /// </summary>
        /// <param name="arg"> The value to convert. </param>
        /// <param name="array"> A byte array (presumably containing OSC binary data) to which the sequence of bytes will be added. </param>
        /// <param name="extPointer"> Points at the destination index for the byte sequence. </param>
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
        /// Converts the specified 32-bit floating-point value into a big-endian sequence of bytes, adds it to the provided byte array at the specified index.
        /// </summary>
        /// <param name="arg"> The value to convert. </param>
        /// <param name="array"> A byte array (presumably containing OSC binary data) to which the sequence of bytes will be added. </param>
        /// <param name="pointer"> Points at the destination index for the byte sequence. </param>
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
        /// Returns the specified 64-bit floating-point value as a big-endian sequence of bytes.
        /// </summary>
        /// <param name="arg"> The value to convert. </param>
        /// <returns> An array of eight bytes. </returns>
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
        /// Converts the specified 64-bit floating-point value into a big-endian sequence of bytes, adds it to the provided byte array at the specified index.
        /// The pointer is passed by reference, its value is increased according to the size of added data.
        /// </summary>
        /// <param name="arg"> The value to convert. </param>
        /// <param name="array"> A byte array (presumably containing OSC binary data) to which the sequence of bytes will be added. </param>
        /// <param name="extPointer"> Points at the destination index for the byte sequence. </param>
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
        /// Converts the specified 64-bit floating-point value into a big-endian sequence of bytes, adds it to the provided byte array at the specified index.
        /// </summary>
        /// <param name="arg"> The value to convert. </param>
        /// <param name="array"> A byte array (presumably containing OSC binary data) to which the sequence of bytes will be added. </param>
        /// <param name="pointer"> Points at the destination index for the byte sequence. </param>
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
        /// Converts the specified string into a sequence of ASCII codes.
        /// </summary>
        /// <param name="arg"> The string to convert. </param>
        /// <returns> A byte array containing a sequence of ASCII codes. </returns>
        public static byte[] GetBytes(string arg)
        {
            // just to simplify conversion lol
            OscString oscString = new OscString(arg);

            return oscString.GetBytes();
        }


        /// <summary>
        /// Converts the specified string into a sequence of ASCII codes and adds it to an existing byte array.
        /// The pointer is passed by reference, its value is increased according to the size of added data.
        /// </summary>
        /// <param name="arg"> The string to convert. </param>
        /// <param name="array"> The byte array (presumably containing OSC binary data) to which the sequence of bytes will be added. </param>
        /// <param name="extPointer"> Points at the destination index for the byte sequence. </param>
        public static void AddBytes(string arg, byte[] array, ref int extPointer)
        {
            // this shouldn't actually create more than one byte array
            OscString oscString = arg;

            oscString.CopyBytesToArray(array, extPointer);

            // shift the external pointer
            extPointer += oscString.OscLength;
        }


        /// <summary>
        /// Converts the specified string into a sequence ASCII codes and adds them to an existing byte array.
        /// </summary>
        /// <param name="arg"> The string to convert. </param>
        /// <param name="array"> The byte array (presumably containing OSC binary data) to which the sequence of bytes will be added. </param>
        /// <param name="pointer"> Points at the destination index for the byte sequence. </param>
        public static void AddBytes(string arg, byte[] array, int pointer)
        {
            // this shouldn't actually create more than one byte array
            OscString oscString = arg;

            oscString.CopyBytesToArray(array, pointer);
        }


        /// <summary>
        /// Returns the size of the specified string, in bytes, when converted to OSC binary data.
        /// </summary>
        /// <param name="arg"> The string to be measured. </param>
        public static int GetOscLength(string arg)
        {
            return arg.Length.NextX4();
        }

        #endregion // STRING


        #region OSC STRING

        /// <summary>
        /// "Converts" the specified OscString into a sequence of ASCII codes.
        /// </summary>
        /// <remarks> This method is just here for consistency. Literally the same thing can be achieved by calling the GetCopyOfBytes method of the OscString. </remarks>
        /// <param name="arg"> The string to convert. </param>
        /// <returns> A byte array containing a sequence of ASCII codes. </returns>
        public static byte[] GetBytes(OscString arg)
        {
            return arg.GetCopyOfBytes();
        }


        /// <summary>
        /// "Converts" an OscString into ASCII codes and adds them to an existing byte array.
        /// The pointer is passed by reference, its value is increased according to the size of added data.
        /// </summary>
        /// <remarks> This method is just here for consistency. Literally the same thing can be achieved by calling the CopyBytesToArray method of the OscString. </remarks>
        /// <param name="arg"> The string to convert. </param>
        /// <param name="array"> The byte array (presumably containing OSC binary data) to which the sequence of bytes will be added. </param>
        /// <param name="extPointer"> Points at the destination index for the byte sequence. </param>
        public static void AddBytes(OscString arg, byte[] array, ref int extPointer)
        {
            arg.CopyBytesToArray(array, extPointer);

            extPointer += arg.OscLength;
        }


        /// <summary>
        /// "Converts" an OscString into ASCII codes and adds them to an existing byte array.
        /// </summary>
        /// <remarks> This method is just here for consistency. Literally the same thing can be achieved by calling the CopyBytesToArray method of the OscString. </remarks>
        /// <param name="arg"> The string to convert. </param>
        /// <param name="array"> The byte array (presumably containing OSC binary data) to which the sequence of bytes will be added. </param>
        /// <param name="pointer"> Points at the destination index for the byte sequence. </param>
        public static void AddBytes(OscString arg, byte[] array, int pointer)
        {
            arg.CopyBytesToArray(array, pointer);
        }


        /// <summary>
        /// Returns the size of the specified OscString, in bytes, when converted to OSC binary data.
        /// </summary>
        /// <param name="arg"> The OscString to be measured. </param>
        public static int GetOscLength(OscString arg)
        {
            return arg.OscLength;
        }

        #endregion // OSC STRING


        #region BLOB

        /// <summary>
        /// Returns a copy of the specified array that is formatted as an OSC binary blob.
        /// </summary>
        /// <param name="arg"> The byte array to format. </param>
        /// <returns> An OSC binary blob - still a byte array but formatted as per OSC Protocol spec: preceded by four bytes recording the length of useful data and 
        /// padded with null bytes to be a multiple of four in length. </returns>
        public static byte[] GetBytes(byte[] arg)
        {
            byte[] resultArray = new byte[GetOscLength(arg)];

            AddBytes(arg, resultArray, 0);

            return resultArray;
        }


        /// <summary>
        /// Copies the contents of the specified array into the provided byte array, formatting it as an OSC binary blob.
        /// The pointer is passed by reference, its value is increased according to the size of added data.
        /// </summary>
        /// <param name="arg"> The byte array to format. </param>
        /// <param name="array"> The byte array (presumably containing OSC binary data) to which the blob will be added. </param>
        /// <param name="extPointer"> Points at the destination index for the blob. </param>
        public static void AddBytes(byte[] arg, byte[] array, ref int extPointer)
        {
            // add length         
            AddBytes(arg.Length, array, ref extPointer);

            // add data
            arg.CopyTo(array, extPointer);

            // move pointer
            extPointer += arg.Length.ThisOrNextX4();

        }


        /// <summary>
        /// Copies the contents of the specified array into the provided byte array, formatting it as an OSC binary blob.
        /// </summary>
        /// <param name="arg"> The byte array to format. </param>
        /// <param name="array"> The byte array (presumably containing OSC binary data) to which the blob will be added. </param>
        /// <param name="pointer"> Points at the destination index for the blob. </param>
        public static void AddBytes(byte[] arg, byte[] array, int pointer)
        {
            int fakePointer = pointer;
            AddBytes(arg, array, ref fakePointer);
        }


        /// <summary>
        /// Returns the size of the specified byte array when formatted as an OSC binary blob.
        /// </summary>
        /// <remarks> This includes the additional four bytes to record the length of data in the blob, and the extra null bytes at the end. </remarks>
        /// <param name="arg"> Byte array to be measured. </param>
        public static int GetOscLength(byte[] arg)
        {
            return arg.Length.ThisOrNextX4() + OscProtocol.Chunk32;
        }

        #endregion // BLOB


        #region TIMETAG
        /// <summary>
        /// Returns the specified OSC Timetag - which is a 64-bit unsigned integer - as a big-endian sequence of bytes.
        /// </summary>
        /// <param name="arg"> The timetag to convert. </param>
        /// <returns> An array of eight bytes. </returns>
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
        /// Converts the specified OSC Timetag - which is a 64-bit unsigned integer - into a big-endian sequence of bytes, adds it to the provided byte array at the specified index.
        /// The pointer is passed by reference, its value is increased according to the size of added data.
        /// </summary>
        /// <param name="arg"> The value to convert. </param>
        /// <param name="array"> The byte array (presumably containing OSC binary data) to which the sequence of bytes will be added. </param>
        /// <param name="extPointer"> Points at the destination index for the byte sequence. </param>
        public static void AddBytes(OscTimetag arg, byte[] array, ref int extPointer)
        {
            AddBytes(arg, array, extPointer);

            // shift the external pointer
            extPointer += OscProtocol.Chunk64;
        }


        /// <summary>
        /// Converts the specified OSC Timetag - which is a 64-bit unsigned integer - into a big-endian sequence of bytes, adds it to the provided byte array at the specified index.
        /// </summary>
        /// <param name="arg"> The value to convert. </param>
        /// <param name="array"> The byte array (presumably containing OSC binary data) to which the sequence of bytes will be added. </param>
        /// <param name="pointer"> Points at the destination index for the byte sequence. </param>
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
        /// <summary>
        /// Returns the specified 32-bit, RGBA color - represented by an OscColor struct - as a sequence of four bytes.
        /// </summary>
        /// <param name="arg"> The OSC Color struct to convert. </param>
        /// <returns> An array of four bytes. </returns>
        public static byte[] GetBytes(OscColor arg)
        {
            byte[] result = new byte[4];

            result[0] = arg.Red;
            result[1] = arg.Green;
            result[2] = arg.Blue;
            result[3] = arg.Alpha;

            return result;
        }


        /// <summary>
        /// Converts the specified 32-bit, RGBA color - represented by an OscColor struct - into a sequence of four bytes, adds them to the provided byte array at the specified index.
        /// The pointer is passed by reference, its value is increased according to the size of added data.
        /// </summary>
        /// <param name="arg"> The OscColor struct to convert. </param>
        /// <param name="array"> The byte array (presumably containing OSC binary data) to which the sequence of bytes will be added. </param>
        /// <param name="extPointer"> Points at the destination index for the byte sequence. </param>
        public static void AddBytes(OscColor arg, byte[] array, ref int extPointer)
        {
            array[extPointer++] = arg.Red;
            array[extPointer++] = arg.Green;
            array[extPointer++] = arg.Blue;
            array[extPointer++] = arg.Alpha;
        }


        /// <summary>
        /// Converts the specified 32-bit, RGBA color - represented by an OscColor struct - into a sequence of four bytes, adds them to the provided byte array at the specified index.
        /// </summary>
        /// <param name="arg"> The OscColor struct to convert. </param>
        /// <param name="array"> The byte array (presumably containing OSC binary data) to which the sequence of bytes will be added. </param>
        /// <param name="pointer"> Points at the destination index for the byte sequence. </param>
        public static void AddBytes(OscColor arg, byte[] array, int pointer)
        {
            int index = pointer;
            AddBytes(arg, array, ref index);
        }

        #endregion // COLOR


        #region MIDI

        /// <summary>
        /// Returns the specified MIDI message - represented by an OscMidi struct - as a sequence of four bytes.
        /// </summary>
        /// <param name="arg"> The OscMidi struct to convert. </param>
        /// <returns> An array of four bytes. </returns>
        public static byte[] GetBytes(OscMidi arg)
        {
            byte[] result = new byte[4];

            result[0] = arg.PortId;
            result[1] = arg.Status;
            result[2] = arg.Data1;
            result[3] = arg.Data2;

            return result;
        }


        /// <summary>
        /// Converts the specified MIDI message - represented by an OscMidi struct - into a sequence of four bytes, adds them to the provided byte array at the specified index.
        /// The pointer is passed by reference, its value is increased according to the size of added data.
        /// </summary>
        /// <param name="arg"> The OscColor struct to convert. </param>
        /// <param name="array"> The byte array (presumably containing OSC binary data) to which the sequence of bytes will be added. </param>
        /// <param name="extPointer"> Points at the destination index for the byte sequence. </param>
        public static void AddBytes(OscMidi arg, byte[] array, ref int extPointer)
        {
            array[extPointer++] = arg.PortId;
            array[extPointer++] = arg.Status;
            array[extPointer++] = arg.Data1;
            array[extPointer++] = arg.Data2;
        }


        /// <summary>
        /// Converts the specified MIDI message - represented by an OscMidi struct - into a sequence of four bytes, adds them to the provided byte array at the specified index.
        /// </summary>
        /// <param name="arg"> The OscColor struct to convert. </param>
        /// <param name="array"> The byte array (presumably containing OSC binary data) to which the sequence of bytes will be added. </param>
        /// <param name="pointer"> Points at the destination index for the byte sequence. </param>
        public static void AddBytes(OscMidi arg, byte[] array, int pointer)
        {
            int index = pointer;
            AddBytes(arg, array, ref index);
        }

        #endregion // MIDI

    }

}
