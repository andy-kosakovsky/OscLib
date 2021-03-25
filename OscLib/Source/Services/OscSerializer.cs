using System;
using System.Collections.Generic;
using System.Text;

namespace OscLib
{
    /// <summary>
    /// Serializes data into bytes according to OSC Protocol.
    /// </summary>
    public static class OscSerializer
    {
        #region ADDING BYTES OF WHOLE MESSAGES / BUNDLES

        /// <summary>
        /// Serializes the provided OSC Message into byte data, adds it to the provided byte array.
        /// </summary>
        /// <param name="message"> The message to convert to bytes. </param>
        /// <param name="array"> The byte array to add the message data to. </param>
        /// <param name="extPointer"> The external pointer designating the index from which to add data to array. Will be shifted forwards by the length of added data. </param>
        /// <exception cref="ArgumentException"> Thrown if the provided data array is too small to fit the message into. </exception>
        public static void AddBytes(OscMessage message, byte[] array, ref int extPointer)
        {
            int addrLength = message.AddressPattern.OscLength;

            int msgStart = extPointer;

            if (msgStart + message.Length > array.Length)
            {
                throw new ArgumentException("OSC Serializer ERROR: Cannot add OSC data to byte array, array is too short. ");
            }

            message.AddressPattern.CopyTo(array, msgStart);

            extPointer += addrLength;

            if (message.Arguments.Length > 0)
            {
                // find the length of type tag, " + 1" accounts for the comma
                int typeTagLength = OscUtil.GetNextMultipleOfFour(message.Arguments.Length + 1);

                array[msgStart + addrLength] = OscProtocol.Comma;

                extPointer = msgStart + addrLength + typeTagLength;

                for (int i = 0; i < message.Arguments.Length; i++)
                {
                    AddBytesArg(message.Arguments[i], array, ref extPointer, out array[msgStart + addrLength + 1 + i]);
                }

            }

        }


        /// <summary>
        /// Serializes the provided OSC Bundle into byte data, adds it to the provided byte array.
        /// </summary>
        /// <param name="bundle"> The bundle to convert to bytes. </param>
        /// <param name="array"> The byte array to add bundle data to. </param>
        /// <param name="extPointer"> The external pointer designating the index from which to add data to array. Will be shifted forwards by the length of added data. </param>
        public static void AddBytes(OscBundle bundle, byte[] array, ref int extPointer)
        {
            int bndStart = extPointer;

            if (bndStart + bundle.Length > array.Length)
            {
                throw new ArgumentException("OSC Serializer ERROR: Cannot add OSC data to byte array, array is too short. ");
            }

            AddBundleHeader(array, extPointer, bundle.Timetag);

            extPointer += OscBundle.HeaderLength;

            // add messages
            if (bundle.Messages.Length > 0)
            {
                AddBytesAsContent(bundle.Messages, array, ref extPointer);
            }

            if (bundle.Bundles.Length > 0)
            {
                AddBytesAsContent(bundle.Bundles, array, ref extPointer);
            }

        }


        /// <summary>
        /// Converts the provided address pattern and arguments into OSC Message byte data, adds it to the provided byte array.
        /// </summary>
        /// <param name="addressPattern"> The address pattern of this message. </param>
        /// <param name="arguments"> The arguments (should be of supported types, obviously). </param>
        /// <param name="array"> The byte array to add the message data to. </param>
        /// <param name="extPointer"> The external pointer designating the index from which to add data to array. Will be shifted forwards by the length of added data. </param>
        /// <exception cref="ArgumentException"> Thrown if the provided data array is too small to fit the message into. </exception>
        public static void NewMessageAddBytes(OscString addressPattern, object[] arguments, byte[] array, ref int extPointer)
        {
            // make a new message to check length, etc. This shouldn't be too problematic memory-wise, as we're not creating anything new on the heap
            OscMessage message = new OscMessage(addressPattern, arguments);

            AddBytes(message, array, ref extPointer);

        }

        #endregion // ADDING BYTES OF WHOLE MESSAGES / BUNDLES



        #region GETTING BYTES OF WHOLE MESSAGES / BUNDLES

        /// <summary>
        /// Serializes the provided OSC Message into a byte array.
        /// </summary>
        /// <param name="message"> OSC Message to be serialized. </param>       
        /// <returns> An OSC Protocol-compliant byte array containing the serialized message. </returns>
        public static byte[] GetBytes(OscMessage message)
        {
            byte[] binaryData = new byte[message.Length];

            int pointer = 0;

            AddBytes(message, binaryData, ref pointer);

            return binaryData;

        }


        /// <summary>
        /// Serializes the provided OSC Bundle into a byte array.
        /// </summary>
        /// <param name="bundle"> OSC Bundle to be serialized. </param>
        /// <returns> An OSC Protocol-compliant byte array containing the serialized bundle. </returns>
        public static byte[] GetBytes(OscBundle bundle)
        {
            byte[] binaryData = new byte[bundle.Length];

            int pointer = 0;

            AddBytes(bundle, binaryData, ref pointer);

            return binaryData;

        }


        /// <summary>
        /// Creates a byte array of OSC binary data out of the provided address pattern and arguments.
        /// </summary>
        /// <param name="addressPattern"> The address pattern of this message. </param>
        /// <param name="arguments"> The arguments (should be of supported types, obviously) </param>
        /// <returns> A byte array containing the serialized message. </returns>
        /// <exception cref="ArgumentNullException"> Thrown if the address pattern is null or empty. </exception>
        /// <exception cref="ArgumentException"> Thrown if the address pattern doesn't start with a "/" symbol, as required per OSC Protocol. </exception>
        public static byte[] NewMessageGetBytes(OscString addressPattern, object[] arguments = null)
        {
            if (OscString.IsNullOrEmpty(addressPattern))
            {
                throw new ArgumentNullException(nameof(addressPattern), "OSC Serializer ERROR: Cannot convert message to bytes, its address pattern is null or empty.");
            }

            // check if the very first symbol of the address pattern is compliant to the standard
            if (addressPattern[0] != OscProtocol.Separator)
            {
                throw new ArgumentException("OSC Serializer ERROR: Cannot convert, provided address pattern doesn't begin with a '/'.");
            }

            return GetBytes(new OscMessage(addressPattern, arguments));

        }

        #endregion // GETTING BYTES OF WHOLE MESSAGES / BUNDLES



        #region ADDING BYTE "CONTENTS"

        /// <summary>
        /// Orders the provided OSC Packets into a byte array of OSC Bundle "contents", adds it to the provided byte array starting from the provided pointer.
        /// </summary>
        /// <param name="packets"> An array of OSC Packets to order. </param>
        /// <param name="array"> The target byte array. </param>
        /// <param name="extPointer"> The data will be added to target array from this index onwards. This pointer will be shifted forwards by the length of added data. </param>
        public static void AddBytesAsContent(OscPacket[] packets, byte[] array, ref int extPointer)
        {

            for (int i = 0; i < packets.Length; i++)
            {
                AddBytes(packets[i].Length, array, ref extPointer);

                packets[i].BinaryData.CopyTo(array, extPointer);
                extPointer += packets[i].Length;

            }

        }


        /// <summary>
        /// Orders the provided OSC Messages into a byte array of OSC Bundle "contents", adds it to the provided byte array starting from the provided pointer.
        /// </summary>
        /// <param name="messages"> An array of OSC Messages to order. </param>
        /// <param name="array"> The target byte array. </param>
        /// <param name="extPointer"> The data will be added to target array from this index onwards. This pointer will be shifted forwards by the length of added data. </param>
        public static void AddBytesAsContent(OscMessage[] messages, byte[] array, ref int extPointer)
        {

            for (int i = 0; i < messages.Length; i++)
            {
                AddBytes(messages[i].Length, array, ref extPointer);

                AddBytes(messages[i], array, ref extPointer);
            }

        }


        /// <summary>
        /// Orders the provided OSC Bundles into a byte array of OSC Bundle "contents", adds it to the provided byte array starting from the provided pointer.
        /// </summary>
        /// <param name="bundles"> An array of OSC Bundles to order. </param>
        /// <param name="array"> The target byte array. </param>
        /// <param name="extPointer"> The data will be added to target array from this index onwards. This pointer will be shifted forwards by the length of added data. </param>
        public static void AddBytesAsContent(OscBundle[] bundles, byte[] array, ref int extPointer)
        {

            for (int i = 0; i < bundles.Length; i++)
            {
                AddBytes(bundles[i].Length, array, ref extPointer);

                AddBytes(bundles[i], array, ref extPointer);
            }

        }

        #endregion // ADDING BYTE "CONTENTS"



        #region GETTING BYTE "CONTENTS"

        /// <summary>
        /// Converts the provided OSC Messages to bytes and orders them into a byte array of OSC Bundle "contents".
        /// </summary>
        /// <param name="messages"> An array of OSC Messages to convert. </param>
        /// <param name="length"> An out parameter containing the length of the resultant byte array. </param>
        /// <returns> An ordered byte array of elements formatted as [element 1's length] - [element 1] - [element 2's length] - [element 2] and so on. </returns>
        public static byte[] GetBytesAsContent(OscMessage[] messages, out int length)
        {
            length = 0;

            for (int i = 0; i < messages.Length; i++)
            {
                // get length of all messages, plus allow 4 bytes for the "length" integer
                length += messages[i].Length + OscProtocol.Chunk32;
            }

            byte[] binaryData = new byte[length];

            int pointer = 0;

            for (int i = 0; i < messages.Length; i++)
            {
                // get length of the message
                AddBytes(messages[i].Length, binaryData, ref pointer);

                // get the message
                AddBytes(messages[i], binaryData, ref pointer);
   
                if (pointer > binaryData.Length)
                {
                    throw new IndexOutOfRangeException("Oh no");
                }

            }

            return binaryData;
            
        }
        

        /// <summary>
        /// Converts the provided OSC Bundles to bytes and orders them into a byte array of OSC Bundle "contents".
        /// </summary>
        /// <param name="bundles"> An array of OSC Messages to convert. </param>
        /// <param name="length"> An out parameter containing the length of the resultant byte array. </param>
        /// <returns> An ordered byte array of elements formatted as [element 1's length] - [element 1] - [element 2's length] - [element 2] and so on. </returns>
        public static byte[] GetBytesAsContent(OscBundle[] bundles, out int length)
        {
            length = 0;

            // find length
            for (int i = 0; i < bundles.Length; i++)
            {
                length += bundles[i].Length + OscProtocol.Chunk32;
            }

            byte[] binaryData = new byte[length];
            int pointer = 0;


            for (int i = 0; i < bundles.Length; i++)
            {
                // get bundle length
                AddBytes(bundles[i].Length, binaryData, ref pointer);

                // get bundle
                AddBytes(bundles[i], binaryData, ref pointer);

            }

            return binaryData;

        }


        /// <summary>
        /// Orders the provided OSC Packets into a byte array of OSC Bundle "contents".
        /// </summary>
        /// <param name="packets"> An array of OSC Packets to order. </param>
        /// <param name="length"> An out parameter containing the length of the resultant byte array. </param>
        /// <returns> An ordered byte array of elements formatted as [element 1's length] - [element 1] - [element 2's length] - [element 2] and so on. </returns>
        public static byte[] GetBytesAsContent(OscPacket[] packets, out int length)
        {
            length = 0;

            // get length
            for (int i = 0; i < packets.Length; i++)
            {
                // length of each packet plus 4 bytes for recording the length itself
                length += packets[i].Length + OscProtocol.Chunk32;
            }

            byte[] binaryData = new byte[length];
            int pointer = 0;

            AddBytesAsContent(packets, binaryData, ref pointer);

            return binaryData;

        }

        #endregion // GETTING BYTE "CONTENTS"



        #region ADDING BUNDLE HEADERS

        /// <summary>
        /// Adds a bundle header to the target byte array, starting from the specified index. Tags it with the "execute immediately" timetag.
        /// </summary>
        /// <remarks>
        /// "Adds" means "overwrites the next 16 bytes with the bundle header", by the way, so it's probably best to make sure there's nothing important there first.
        /// </remarks>
        /// <param name="target"> Target byte array to modify. </param>
        /// <param name="index"> Target index. </param>
        public static void AddBundleHeader(byte[] target, int index)
        {

            if (target.Length <= index + OscBundle.HeaderLength)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "OSC Serializer ERROR: Can't add bundle header to byte array at index " + index + ", it won't fit. ");
            }

            OscProtocol.CopyBundleStringTo(target, index);
            OscTime.ImmediatelyBytes.CopyTo(target, index + 8);

        }


        /// <summary>
        /// Adds a bundle header to the target byte array, starting from the specified index and using the provided timetag. 
        /// </summary>
        /// <remarks>
        /// "Adds" means "overwrites the next 16 bytes with the bundle header", by the way, so it's probably best to make sure there's nothing important there first.
        /// </remarks>
        /// <param name="target"> Target byte array to modify. </param>
        /// <param name="index"> Target index. </param>
        /// <param name="timetag"> To be included in the header. </param>
        public static void AddBundleHeader(byte[] target, int index, OscTimetag timetag)
        {

            if (target.Length <= index + OscBundle.HeaderLength)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "OSC Serializer ERROR: Can't add bundle header to byte array at index " + index + ", it won't fit. ");
            }

            OscProtocol.CopyBundleStringTo(target, index);
            GetBytes(timetag).CopyTo(target, index + 8);

        }

        #endregion // ADDING BUNDLE HEADERS



        #region INT32

        /// <summary>
        /// Converts an integer into a byte array, swapping its endianness if needed.
        /// </summary>
        /// <param name="arg"> An integer to be converted. </param>
        /// <returns> A byte array. </returns>
        public static byte[] GetBytes(int arg)
        {
            byte[] data = BitConverter.GetBytes(arg);

            if (BitConverter.IsLittleEndian)
            {
                OscUtil.SwapEndian(data);
            }

            return data;
        }


        /// <summary>
        /// Converts an integer into bytes and adds them to an existing array, swapping their endianness if needed.
        /// </summary>
        /// <param name="arg"> An integer to be converted. </param>
        /// <param name="array"> The target byte array. </param>
        /// <param name="extPointer">The index from which to add data. Will be shifted forwards by the length of added data. </param>
        public static void AddBytes(int arg, byte[] array, ref int extPointer)
        {
            BitConverter.GetBytes(arg).CopyTo(array, extPointer);

            if (BitConverter.IsLittleEndian)
            {
                OscUtil.SwapEndian(array, extPointer, OscProtocol.Chunk32);
            }

            // shift the external pointer
            extPointer += OscProtocol.Chunk32;

        }


        /// <summary>
        /// Converts an integer into a byte array, swapping its endianness if needed. Provides the OSC INT32 type tag.
        /// </summary>
        /// <param name="arg"> An integer to be converted. </param>
        /// <param name="typeTag"> Corresponding OSC type tag. </param>
        /// <returns> A byte array. </returns>
        public static byte[] GetBytesArg(int arg, out byte typeTag)
        {         
            typeTag = OscProtocol.TypeTagInt32;

            return GetBytes(arg);
        }


        /// <summary>
        /// Converts an integer into bytes and adds them to an existing array, swapping their endianness if needed. Provides the OSC INT32 type tag.
        /// </summary>
        /// <param name="arg"> An integer to be converted. </param>
        /// <param name="array"> The target byte array. </param>
        /// <param name="extPointer"> The index from which to add data. Will be shifted forwards by the length of added data. </param>
        /// <param name="typeTag"> Corresponding OSC type tag. </param>
        public static void AddBytesArg(int arg, byte[] array, ref int extPointer, out byte typeTag)
        {
            typeTag = OscProtocol.TypeTagInt32;

            AddBytes(arg, array, ref extPointer);

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
            byte[] data = BitConverter.GetBytes(arg);

            if (BitConverter.IsLittleEndian)
            {
                OscUtil.SwapEndian(data);
            }

            return data;
        }


        /// <summary>
        /// Converts a longint into bytes and adds them to an existing array, swapping their endianness if needed.
        /// </summary>
        /// <param name="arg"> A longint to be converted. </param>
        /// <param name="array"> The target byte array. </param>
        /// <param name="extPointer"> The index from which to add data. Will be shifted forwards by the length of added data.  </param>
        public static void AddBytes(long arg, byte[] array, ref int extPointer)
        {
            BitConverter.GetBytes(arg).CopyTo(array, extPointer);

            if (BitConverter.IsLittleEndian)
            {
                OscUtil.SwapEndian(array, extPointer, OscProtocol.Chunk64);
            }

            // shift the external pointer
            extPointer += OscProtocol.Chunk64;

        }


        /// <summary>
        /// Converts an longint into a byte array, swapping its endianness if needed. Provides the OSC INT64 type tag.
        /// </summary>
        /// <param name="arg"> Longint to be converted. </param>
        /// <param name="typeTag"> Corresponding OSC type tag. </param>
        /// <returns> A byte array. </returns>
        public static byte[] GetBytesArg(long arg, out byte typeTag)
        {
            typeTag = OscProtocol.TypeTagInt64;

            return GetBytes(arg);
        }


        /// <summary>
        /// Converts a longint into bytes and adds them to an existing array, swapping their endianness if needed. Provides the OSC INT64 type tag.
        /// </summary>
        /// <param name="arg"> An integer to be converted. </param>
        /// <param name="array"> The target byte array. </param>
        /// <param name="extPointer"> The index from which to add data. Will be shifted forwards by the length of added data. </param>
        /// <param name="typeTag"> Corresponding OSC type tag. </param>
        public static void AddBytesArg(long arg, byte[] array, ref int extPointer, out byte typeTag)
        {
            typeTag = OscProtocol.TypeTagInt64;

            AddBytes(arg, array, ref extPointer);

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
            byte[] data = BitConverter.GetBytes(arg);

            if (BitConverter.IsLittleEndian)
            {
                OscUtil.SwapEndian(data);
            }

            return data;
        }


        /// <summary>
        /// Converts a float into bytes and adds them to an existing array, swapping their endianness if needed.
        /// </summary>
        /// <param name="arg"> The float to be converted. </param>
        /// <param name="array"> The target byte array. </param>
        /// <param name="extPointer">The index from which to add data. Will be shifted forwards by the length of added data. </param>
        public static void AddBytes(float arg, byte[] array, ref int extPointer)
        {
            BitConverter.GetBytes(arg).CopyTo(array, extPointer);

            if (BitConverter.IsLittleEndian)
            {
                OscUtil.SwapEndian(array, extPointer, OscProtocol.Chunk32);
            }

            // shift the external pointer
            extPointer += OscProtocol.Chunk32;
        }


        /// <summary>
        /// Converts a float into a byte array, swapping its endianness if needed. Provides the OSC FLOAT32 type tag.
        /// </summary>
        /// <param name="arg"> The float to be converted. </param>
        /// <param name="typeTag"> Corresponding OSC type tag. </param>
        /// <returns> A byte array. </returns>
        public static byte[] GetBytesArg(float arg, out byte typeTag)
        {
            typeTag = OscProtocol.TypeTagFloat32;

            return GetBytes(arg);
        }


        /// <summary>
        /// Converts a float into bytes and adds them to an existing array, swapping their endianness if needed. Provides the OSC FLOAT32 type tag.
        /// </summary>
        /// <param name="arg"> The float to be converted. </param>
        /// <param name="array"> The target byte array. </param>
        /// <param name="extPointer"> The index from which to add data. Will be shifted forwards by the length of added data. </param>
        /// <param name="typeTag"> Corresponding OSC type tag. </param>
        public static void AddBytesArg(float arg, byte[] array, ref int extPointer, out byte typeTag)
        {
            typeTag = OscProtocol.TypeTagFloat32;

            AddBytes(arg, array, ref extPointer);
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
            byte[] data = BitConverter.GetBytes(arg);

            if (BitConverter.IsLittleEndian)
            {
                OscUtil.SwapEndian(data);
            }

            return data;
        }


        /// <summary>
        /// Converts a double into bytes and adds them to an existing array, swapping their endianness if needed.
        /// </summary>
        /// <param name="arg"> The double to be converted. </param>
        /// <param name="array"> The target byte array. </param>
        /// <param name="extPointer">The index from which to add data. Will be shifted forwards by the length of added data. </param>
        public static void AddBytes(double arg, byte[] array, ref int extPointer)
        {
            BitConverter.GetBytes(arg).CopyTo(array, extPointer);

            if (BitConverter.IsLittleEndian)
            {
                OscUtil.SwapEndian(array, extPointer, OscProtocol.Chunk64);
            }

            // shift the external pointer
            extPointer += OscProtocol.Chunk64;

        }


        /// <summary>
        /// Converts a double into a byte array, swapping its endianness if needed. Provides the OSC FLOAT64 type tag.
        /// </summary>
        /// <param name="arg"> The double to be converted. </param>
        /// <param name="typeTag"> Corresponding OSC type tag. </param>
        /// <returns> A byte array. </returns>
        public static byte[] GetBytesArg(double arg, out byte typeTag)
        {
            typeTag = OscProtocol.TypeTagFloat64;

            return GetBytes(arg);
        }


        /// <summary>
        /// Converts a double into bytes and adds them to an existing array, swapping their endianness if needed. Provides the OSC FLOAT64 type tag.
        /// </summary>
        /// <param name="arg"> The double to be converted. </param>
        /// <param name="array"> The target byte array. </param>
        /// <param name="extPointer"> The index from which to add data. Will be shifted forwards by the length of added data. </param>
        /// <param name="typeTag"> Corresponding OSC type tag. </param>
        public static void AddBytesArg(double arg, byte[] array, ref int extPointer, out byte typeTag)
        {
            typeTag = OscProtocol.TypeTagFloat64;

            AddBytes(arg, array, ref extPointer);
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

            return oscString.OscBytes;
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
        /// Converts a string into an ASCII byte array. Provides the OSC STRING type tag.
        /// </summary>
        /// <param name="arg"> The string to be converted. </param>
        /// <param name="typeTag"> Corresponding OSC type tag. </param>
        /// <returns> A byte array. </returns>
        public static byte[] GetBytesArg(string arg, out byte typeTag)
        {
            typeTag = OscProtocol.TypeTagString;

            return GetBytes(arg);
        }


        /// <summary>
        /// Converts a string into ASCII bytes and adds them to an existing array. Provides the OSC STRING type tag.
        /// </summary>
        /// <param name="arg"> The string to be converted. </param>
        /// <param name="array"> The target byte array. </param>
        /// <param name="extPointer"> The index from which to add data. Will be shifted forwards by the length of added data. </param>
        /// <param name="typeTag"> Corresponding OSC type tag. </param>
        public static void AddBytesArg(string arg, byte[] array, ref int extPointer, out byte typeTag)
        {
            typeTag = OscProtocol.TypeTagString;

            AddBytes(arg, array, ref extPointer);
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
            return arg.OscBytes;
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
        /// Converts a string into an ASCII byte array. Provides the OSC-string type tag.
        /// </summary>
        /// <param name="arg"> String to be converted. </param>
        /// <param name="typeTag"> Corresponding OSC type tag. </param>
        /// <returns> A byte array. </returns>
        public static byte[] GetBytesArg(OscString arg, out byte typeTag)
        {
            typeTag = OscProtocol.TypeTagString;

            return GetBytes(arg);
        }


        /// <summary>
        /// Converts an OSC String into ASCII bytes and adds them to an existing array. Provides the OSC STRING type tag.
        /// </summary>
        /// <param name="arg"> The string to be converted. </param>
        /// <param name="array"> The target byte array. </param>
        /// <param name="extPointer"> The index from which to add data. Will be shifted forwards by the length of added data. </param>
        /// <param name="typeTag"> Corresponding OSC type tag. </param>
        public static void AddBytesArg(OscString arg, byte[] array, ref int extPointer, out byte typeTag)
        {
            typeTag = OscProtocol.TypeTagString;

            AddBytes(arg, array, ref extPointer);
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

            // copy the length into the array
            GetBytes(arg.Length).CopyTo(resultArray, 0);

            arg.CopyTo(resultArray, OscProtocol.Chunk32);

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
        /// Converts a byte array into an OSC Protocol-compliant binary blob and returns it as a copy. Provides the OSC BLOB type tag.
        /// </summary>
        /// <param name="arg"> The byte array to be converted. </param>
        /// <param name="typeTag"> Corresponding OSC type tag. </param>
        /// <returns> A binary blob - still a byte array but correctly formatted. </returns>
        public static byte[] GetBytesArg(byte[] arg, out byte typeTag)
        {
            typeTag = OscProtocol.TypeTagBlob;

            return GetBytes(arg);
        }


        /// <summary>
        /// Formats a byte array to be OSC Protocol-compliant and adds it to an existing byte array. Provides the OSC BLOB type tag.
        /// </summary>
        /// <param name="arg"> The byte array to be converted. </param>
        /// <param name="array"> The target byte array. </param>
        /// <param name="extPointer"> The index from which to add data. Will be shifted forwards by the length of added data. </param>
        /// <param name="typeTag"> Corresponding OSC type tag. </param>
        public static void AddBytesArg(byte[] arg, byte[] array, ref int extPointer, out byte typeTag)
        {
            typeTag = OscProtocol.TypeTagBlob;

            AddBytes(arg, array, ref extPointer);
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



        #region BOOL
        
        /// <summary>
        /// Converts a bool into byte representation. Returns the type tag. Behaviour is dependant on OSC Protocol attributes.
        /// </summary>
        /// <param name="arg"> Bool to be converted. </param>
        /// <param name="typeTag"> Corresponding OSC type tag. </param>
        /// <returns> A byte array. </returns>
        public static byte[] GetBytesArg(bool arg, out byte typeTag)
        {
            // TODO: the behaviour here should be controlled by the settings - this can be either represented as 0 for false and 1 for true, or "T" and "F" in the type tag string.

            if (arg)
                return GetBytesArg(1, out typeTag);
            else
                return GetBytesArg(0, out typeTag);
        }


        public static void AddBytesArg(bool arg, byte[] array, ref int extPointer, out byte typeTag)
        {
            if (arg)
                AddBytesArg(1, array, ref extPointer, out typeTag);
            else
                AddBytesArg(0, array, ref extPointer, out typeTag);

        }

        #endregion



        #region TIMETAG
        /// <summary>
        /// Converts an OSC Timetag into a byte array.
        /// </summary>
        /// <param name="timetag"> The timetag to be converted. </param>
        /// <returns> A byte array. </returns>
        public static byte[] GetBytes(OscTimetag timetag)
        {
            byte[] data = BitConverter.GetBytes(timetag.NtpTimestamp);

            if (BitConverter.IsLittleEndian)
            {
                OscUtil.SwapEndian(data);
            }

            return data;
        }


        /// <summary>
        /// Converts an OSC Timetag into bytes and adds them to an existing array.
        /// </summary>
        /// <param name="arg"> The timetag to be converted. </param>
        /// <param name="array"> The target byte array. </param>
        /// <param name="extPointer">The index from which to add data. Will be shifted forwards by the length of added data. </param>
        public static void AddBytes(OscTimetag arg, byte[] array, ref int extPointer)
        {
            BitConverter.GetBytes(arg.NtpTimestamp).CopyTo(array, extPointer);

            if (BitConverter.IsLittleEndian)
            {
                OscUtil.SwapEndian(array, extPointer, OscProtocol.Chunk32);
            }

            // shift the external pointer
            extPointer += OscProtocol.Chunk64;

        }

        /// <summary>
        /// Converts an OSC Timetag into a byte array. Provides the OSC timestamp type tag.
        /// </summary>
        /// <param name="timetag"> The timetag to be converted. </param>
        /// <param name="typeTag"> Corresponding OSC type tag. </param>
        /// <returns> A byte array. </returns>
        public static byte[] GetBytesArg(OscTimetag timetag, out byte typeTag)
        {
            byte[] data = BitConverter.GetBytes(timetag.NtpTimestamp);

            if (BitConverter.IsLittleEndian)
            {
                OscUtil.SwapEndian(data);
            }

            typeTag = OscProtocol.TypeTagTime;

            return data;
        }


        /// <summary>
        /// Converts an OSC Timetag into bytes and adds them to an existing array. Provides the OSC TIMETAG type tag.
        /// </summary>
        /// <param name="arg"> The timetag to be converted. </param>
        /// <param name="array"> The target byte array. </param>
        /// <param name="extPointer"> The index from which to add data. Will be shifted forwards by the length of added data. </param>
        /// <param name="typeTag"> Corresponding OSC type tag. </param>
        public static void AddBytesArg(OscTimetag arg, byte[] array, ref int extPointer, out byte typeTag)
        {
            typeTag = OscProtocol.TypeTagTime;

            AddBytes(arg, array, ref extPointer);
        }

        #endregion



        #region GENERIC METHODS

        /// <summary>
        /// The catch-all method to serialize OSC Protocol-supported message arguments into byte arrays.  
        /// </summary>
        /// <remarks>
        /// Implemented the way it is to minimize boxing when dealing with objects and vars of unknown type.
        /// </remarks>
        /// <param name="arg"> Argument to be serialized. </param>
        /// <param name="typeTag"> Will return the OSC type tag for the provided argument. </param>
        /// <returns> The byte array containing the argument converted to OSC Protocol-compliant binary. </returns>
        /// <exception cref="ArgumentException"> Thrown when the argument is of an unsupported type. </exception>
        public static byte[] GetBytesArg<T>(T arg, out byte typeTag)
        {
            switch (arg)
            {
                case int argInt:
                    return GetBytesArg(argInt, out typeTag);

                case long argLong:
                    return GetBytesArg(argLong, out typeTag);

                case float argFloat:
                    return GetBytesArg(argFloat, out typeTag);

                case double argDouble:
                    return GetBytesArg(argDouble, out typeTag);

                case string argString:
                    return GetBytesArg(argString, out typeTag);

                case OscString oscString:
                    return GetBytesArg(oscString, out typeTag);
                    
                case byte[] argByte:
                    return GetBytesArg(argByte, out typeTag);

                case bool argBool:
                    return GetBytesArg(argBool, out typeTag);

                case char argChar:
                    return GetBytesArg(argChar.ToString(), out typeTag);

                case OscTimetag argTimestamp:
                    return GetBytesArg(argTimestamp, out typeTag);

                default:
                    throw new ArgumentException("Command Error: Argument " + arg.ToString() + " of unsupported type.");

            }

        }


        /// <summary>
        /// The catch-all method to serialize OSC Protocol-supported message arguments and add them into existing byte arrays.  
        /// </summary>
        /// <remarks>
        /// Implemented the way it is to minimize boxing when dealing with objects and vars of unknown type.
        /// </remarks>
        /// <param name="arg"> The argument to be serialized. </param>
        /// <param name="array"> The target byte array. </param>
        /// <param name="extPointer"> The index from which to add data. Will be shifted forwards by the length of added data. </param>
        /// <param name="typeTag"> Corresponding OSC type tag. </param>
        /// <exception cref="ArgumentException"> Thrown when the argument is of an unsupported type. </exception>
        public static void AddBytesArg<T>(T arg, byte[] array, ref int extPointer, out byte typeTag)
        {
            switch (arg)
            {
                case int argInt:
                    AddBytesArg(argInt, array, ref extPointer, out typeTag);
                    break;

                case long argLong:
                    AddBytesArg(argLong, array, ref extPointer, out typeTag);
                    break;

                case float argFloat:
                    AddBytesArg(argFloat, array, ref extPointer, out typeTag);
                    break;

                case double argDouble:
                    AddBytesArg(argDouble, array, ref extPointer, out typeTag);
                    break;

                case string argString:
                    AddBytesArg(argString, array, ref extPointer, out typeTag);
                    break;

                case OscString oscString:
                    AddBytesArg(oscString, array, ref extPointer, out typeTag);
                    break;

                case byte[] argByte:
                    AddBytesArg(argByte, array, ref extPointer, out typeTag);
                    break;

                case bool argBool:
                    AddBytesArg(argBool, array, ref extPointer, out typeTag);
                    break;

                case char argChar:
                    AddBytesArg(argChar.ToString(), array, ref extPointer, out typeTag);
                    break;

                case OscTimetag argTimetag:
                    AddBytesArg(argTimetag, array, ref extPointer, out typeTag);
                    break;

                default:
                    throw new ArgumentException("Command Error: Argument " + arg.ToString() + " of unsupported type.");

            }

        }

        /// <summary>
        /// The catch-all method to get OSC-supported argument lengths in bytes.
        /// </summary> 
        /// <param name="arg"> The argument to check the byte length of. </param>
        /// <returns> Byte length of the argument. </returns>
        /// <exception cref="ArgumentException"> Thrown when the argument is of an unsupported type. </exception>
        public static int GetLength<T>(T arg)
        {
            switch (arg)
            {
                case int _:
                    return OscProtocol.Chunk32;

                case long _:
                    return OscProtocol.Chunk64;

                case float _:
                    return OscProtocol.Chunk32;

                case double _:
                    return OscProtocol.Chunk64;

                case string argString:
                    return GetLength(argString);

                case OscString oscString:
                    return GetLength(oscString);

                case byte[] argByte:
                    return GetLength(argByte);

                case bool _:
                    return OscProtocol.Chunk32;

                case char _:
                    return OscProtocol.Chunk32;

                case OscTimetag _:
                    return OscProtocol.Chunk64;

                case OscBundle oscBundle:
                    return oscBundle.Length;

                case OscMessage oscMessage:
                    return oscMessage.Length;


                default:
                    throw new ArgumentException("Command Error: Argument " + arg.ToString() + " of unsupported type.");

            }

        }

        #endregion // GENERIC METHODS


    }

}
