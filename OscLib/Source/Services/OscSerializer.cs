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

            // + 1 accounts for the "," symbol in the type tag string
            int typeTagBinLength = 0;
            int argBinLength = 0;

            bool argsPresent = (arguments != null) && (arguments.Length > 0);

            if (argsPresent)
            {
                typeTagBinLength = OscUtil.GetNextMultipleOfFour(arguments.Length + 1);

                // get the binary length of the arg array
                for (int i = 0; i < arguments.Length; i++)
                {
                    argBinLength += GetByteLength(arguments[i]);
                }

            }
            
            // create binary data array of appropriate length
            byte[] binaryData = new byte[addressPattern.OscLength + typeTagBinLength + argBinLength];

            addressPattern.CopyTo(binaryData, 0);


            if (argsPresent)
            {
                // set the first character of the type tag string to be the separator

                binaryData[addressPattern.OscLength] = OscProtocol.Comma;

                int currentPos = addressPattern.OscLength + typeTagBinLength;

                for (int i = 0; i < arguments.Length; i++)
                {
                    GetBytesArg(arguments[i], out binaryData[addressPattern.OscLength + i + 1]).CopyTo(binaryData, currentPos);
                    currentPos += GetByteLength(arguments[i]);
                }

            }

            return binaryData;

        }


        /// <summary>
        /// Creates a byte array of OSC binary data using the provided OSC Message.
        /// </summary>
        /// <param name="message"> OSC Message to be serialized. </param>       
        /// <returns> An OSC Protocol-compliant byte array containing the serialized message. </returns>
        public static byte[] GetBytes(OscMessage message)
        {
            return NewMessageGetBytes(message.AddressPattern, message.Arguments);
        }


        /// <summary>
        /// Creates a byte array of OSC binary data using the provided OSC Bundle. 
        /// </summary>
        /// <param name="bundle"> OSC Bundle to be serialized. </param>
        /// <returns> An OSC Protocol-compliant byte array containing the serialized bundle. </returns>
        public static byte[] GetBytes(OscBundle bundle)
        {
            byte[] binaryData = new byte[bundle.Length];

            AddBundleHeader(binaryData, 0, bundle.Timetag);

            int pointer = OscBundle.HeaderLength;

            // add messages
            GetBytes(bundle.Messages, out int msgLength).CopyTo(binaryData, pointer);

            pointer += msgLength;

            // then add bundles 
            // TODO: maybe it's possible to avoid recursion here? would be nice
            for (int i = 0; i < bundle.Bundles.Length; i++)
            {
                // get length
                GetBytes(bundle.Bundles[i].Length).CopyTo(binaryData, pointer);
                pointer += OscProtocol.Chunk32;

                // get bundle
                GetBytes(bundle.Bundles[i]).CopyTo(binaryData, pointer);
                pointer += bundle.Bundles[i].Length;

            }

            return binaryData;

        }


        /// <summary>
        /// Creates a byte array of OSC binary data out of the provided messages - their lengths included. 
        /// </summary>
        /// <param name="messages"> An array of OSC Messages to pack. </param>
        /// <returns></returns>
        public static byte[] GetBytes(OscMessage[] messages, out int length)
        {
            if (messages == null)
            {
                throw new ArgumentNullException(nameof(messages));
            }

            length = 0;

            for (int i = 0; i < messages.Length; i++)
            {
                // get length of all messages, plus allow 4 bytes for the "length" integer
                length += messages[i].Length + OscProtocol.Chunk32;
            }

            byte[] data = new byte[length];

            int pointer = 0;

            for (int i = 0; i < messages.Length; i++)
            {
                // get length of the message
                GetBytes(messages[i].Length).CopyTo(data, pointer);
                pointer += OscProtocol.Chunk32;

                // get the message
                GetBytes(messages[i]).CopyTo(data, pointer);
                pointer += messages[i].Length;

                if (pointer > data.Length)
                {
                    throw new IndexOutOfRangeException("Oh no");
                }
            }

            return data;
            
        }


        public static byte[] GetBytes(OscBundle[] bundles)
        {
            throw new NotImplementedException();
        }


        public static byte[] GetBytes(object[] messagesBundles)
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// Adds a bundle header to the target byte array, starting from the specified index. Tags it with the "execute immediately" timetag.
        /// </summary>
        /// <param name="target"> Target byte array to modify. </param>
        /// <param name="index"> Target index. </param>
        public static void AddBundleHeader(byte[] target, int index)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

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
        /// <param name="target"> Target byte array to modify. </param>
        /// <param name="index"> Target index. </param>
        /// <param name="timetag"> To be included in the header. </param>
        public static void AddBundleHeader(byte[] target, int index, OscTimetag timetag)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (target.Length <= index + OscBundle.HeaderLength)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "OSC Serializer ERROR: Can't add bundle header to byte array at index " + index + ", it won't fit. ");
            }

            OscProtocol.CopyBundleStringTo(target, index);
            GetBytes(timetag).CopyTo(target, index + 8);

        }


        

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
        /// Converts an integer into a byte array, swapping its endianness if needed. Provides the OSC float32 type tag.
        /// </summary>
        /// <param name="arg"> An integer to be converted. </param>
        /// <param name="typeTag"> Corresponding OSC type tag. </param>
        /// <returns> A byte array. </returns>
        public static byte[] GetBytesArg(int arg, out byte typeTag)
        {         
            typeTag = OscProtocol.TypeTagInt32;

            return GetBytes(arg);
        }

        #endregion


        #region FLOAT32

        /// <summary>
        /// Converts a float into a byte array, swapping its endianness if needed.
        /// </summary>
        /// <param name="arg"> A float to be converted. </param>
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
        /// Converts a float into a byte array, swapping its endianness if needed. Provides the OSC float32 type tag.
        /// </summary>
        /// <param name="arg"> A float to be converted. </param>
        /// <param name="typeTag"> Corresponding OSC type tag. </param>
        /// <returns> A byte array. </returns>
        public static byte[] GetBytesArg(float arg, out byte typeTag)
        {
            typeTag = OscProtocol.TypeTagFloat32;

            return GetBytes(arg);
        }

        #endregion


        #region FLOAT64

        /// <summary>
        /// Converts a double into a byte array, swapping its endianness if needed.
        /// </summary>
        /// <param name="arg"> Double to be converted. </param>
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
        /// Converts a double into a byte array, swapping its endianness if needed. Provides the OSC float64 type tag.
        /// </summary>
        /// <param name="arg"> Double to be converted. </param>
        /// <param name="typeTag"> Corresponding OSC type tag. </param>
        /// <returns> A byte array. </returns>
        public static byte[] GetBytesArg(double arg, out byte typeTag)
        {
            typeTag = OscProtocol.TypeTagFloat64;

            return GetBytes(arg);
        }

        #endregion


        #region STRING

        /// <summary>
        /// Converts a string into an ASCII byte array.
        /// </summary>
        /// <param name="arg"> String to be converted. </param>
        /// <returns> A byte array. </returns>
        public static byte[] GetBytes(string arg)
        {
            OscString oscString = arg;
            
            return GetBytes(oscString);
        }

        /// <summary>
        /// Converts a string into an ASCII byte array. Provides the OSC-string type tag.
        /// </summary>
        /// <param name="arg"> String to be converted. </param>
        /// <param name="typeTag"> Corresponding OSC type tag. </param>
        /// <returns> A byte array. </returns>
        public static byte[] GetBytesArg(string arg, out byte typeTag)
        {
            typeTag = OscProtocol.TypeTagString;

            return GetBytes(arg);
        }

        /// <summary>
        /// Returns the OSC protocol-compliant byte length of the string.
        /// </summary>
        /// <param name="arg"> String to be measured. </param>
        /// <returns> OSC length of the string. </returns>
        public static int GetByteLength(string arg)
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
        /// Converts a string into an ASCII byte array. Provides the OSC-string type tag.
        /// </summary>
        /// <param name="arg"> String to be converted. </param>
        /// <param name="typeTag"> Corresponding OSC type tag. </param>
        /// <returns> A byte array. </returns>
        public static byte[] GetBytes(OscString arg, out byte typeTag)
        {
            typeTag = OscProtocol.TypeTagString;

            return GetBytes(arg);

        }

        /// <summary>
        /// Returns the OSC protocol-compliant byte length of the string.
        /// </summary>
        /// <param name="arg"> String to be measured. </param>
        /// <returns> OSC length of the string. </returns>
        public static int GetByteLength(OscString arg)
        {
            return arg.OscLength;
        }

        #endregion


        #region BLOB

        /// <summary>
        /// Converts a byte array into a OSC protocol-compliant binary blob.
        /// </summary>
        /// <param name="arg"> Byte array to be converted. </param>
        /// <returns> A binary blob - still a byte array but correctly formatted. </returns>
        public static byte[] GetBytes(byte[] arg)
        {
            byte[] resultArray = new byte[GetByteLength(arg)];

            // copy the length into the array
            GetBytes(arg.Length).CopyTo(resultArray, 0);

            arg.CopyTo(resultArray, 4);

            return resultArray;
        }

        /// <summary>
        /// Converts a byte array into a OSC protocol-compliant binary blob. Provides the OSC-blob type tag.
        /// </summary>
        /// <param name="arg"> Byte array to be converted. </param>
        /// <param name="typeTag"> Corresponding OSC type tag. </param>
        /// <returns> A binary blob - still a byte array but correctly formatted. </returns>
        public static byte[] GetBytesArg(byte[] arg, out byte typeTag)
        {
            typeTag = OscProtocol.TypeTagBlob;

            return GetBytes(arg);
        }

        /// <summary>
        /// Returns the OSC protocol-compliant length of the byte array.
        /// </summary>
        /// <param name="arg"> Byte array to be measured. </param>
        /// <returns> OSC length of the array. </returns>
        public static int GetByteLength(byte[] arg)
        {
            return OscUtil.GetNextMultipleOfFour(arg.Length + 4);
        }

        #endregion


        #region BOOL

        /// <summary>
        /// Converts a bool into byte representations of 1 if true and 0 if false.
        /// </summary>
        /// <param name="arg"> Bool to be converted. </param>
        /// <returns> A byte array. </returns>
        public static byte[] GetBytes(bool arg)
        {
            if (arg)
                return GetBytes(1);
            else
                return GetBytes(0);
        }

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

        #endregion


        #region INT64

        /// <summary>
        /// Converts a longint into a byte array, swapping its endianness if needed.
        /// </summary>
        /// <param name="arg"> Longint to be converted. </param>
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
        /// Converts an longint into a byte array, swapping its endianness if needed. Provides an OSC int64 type tag.
        /// </summary>
        /// <param name="arg"> Longint to be converted. </param>
        /// <param name="typeTag"> Corresponding OSC type tag. </param>
        /// <returns> A byte array. </returns>
        public static byte[] GetBytesArg(long arg, out byte typeTag)
        {
            typeTag = OscProtocol.TypeTagInt64;

            return GetBytes(arg);
        }

        #endregion


        #region TIMESTAMP
        /// <summary>
        /// Converts an OSC timestamp into a byte array.
        /// </summary>
        /// <param name="timestamp"> Timestamp to be converted. </param>
        /// <returns> A byte array. </returns>
        public static byte[] GetBytes(OscTimetag timestamp)
        {
            byte[] data = BitConverter.GetBytes(timestamp.NtpTimestamp);

            if (BitConverter.IsLittleEndian)
            {
                OscUtil.SwapEndian(data);
            }

            return data;
        }

        /// <summary>
        /// Converts an OSC timestamp into a byte array. Provides the OSC timestamp type tag.
        /// </summary>
        /// <param name="timestamp"> Timestamp to be converted. </param>
        /// <param name="typeTag"> Corresponding OSC type tag. </param>
        /// <returns> A byte array. </returns>
        public static byte[] GetBytesArg(OscTimetag timestamp, out byte typeTag)
        {
            byte[] data = BitConverter.GetBytes(timestamp.NtpTimestamp);

            if (BitConverter.IsLittleEndian)
            {
                OscUtil.SwapEndian(data);
            }

            typeTag = OscProtocol.TypeTagTime;

            return data;
        }

        #endregion


        /// <summary>
        /// The catch-all method to serialize OSC-supported arguments into byte arrays.  
        /// </summary>
        /// <remarks>
        /// Implemented this way to minimize boxing when dealing with objects and vars of unknown type.
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
                    return GetBytes(oscString, out typeTag);
                    
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
        /// The catch-all method to get OSC-supported argument lengths in bytes.
        /// </summary> 
        /// <param name="arg"> The argument to check the byte length of. </param>
        /// <returns> Byte length of the argument. </returns>
        /// <exception cref="ArgumentException"> Thrown when the argument is of an unsupported type. </exception>
        public static int GetByteLength<T>(T arg)
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
                    return GetByteLength(argString);

                case OscString oscString:
                    return GetByteLength(oscString);

                case byte[] argByte:
                    return GetByteLength(argByte);

                case bool _:
                    return OscProtocol.Chunk32;

                case char _:
                    return OscProtocol.Chunk32;

                case OscTimetag _:
                    return OscProtocol.Chunk64;

                default:
                    throw new ArgumentException("Command Error: Argument " + arg.ToString() + " of unsupported type.");

            }

        }

    }

}
