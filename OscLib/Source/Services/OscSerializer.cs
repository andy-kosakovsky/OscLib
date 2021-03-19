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
        /// Creates a byte array of OSC binary data containing an OSC Message using the provided address pattern and arguments.
        /// </summary>
        /// <param name="addressPattern"> The address pattern of this message. </param>
        /// <param name="arguments"> The arguments (should be of supported types, obviously) </param>
        /// <returns> A byte array containing the serialized message. </returns>
        /// <exception cref="ArgumentNullException"> Thrown if the address pattern is null or empty. </exception>
        /// <exception cref="ArgumentException"> Thrown if the address pattern doesn't start with a "/" symbol, as required per OSC Protocol. </exception>
        public static byte[] GetBytesMessage(OscString addressPattern, object[] arguments)
        {
            if (OscString.IsNullOrEmpty(addressPattern))
            {
                throw new ArgumentNullException(nameof(addressPattern), "OSC Serializer ERROR: Cannot convert message to bytes, its address pattern is null or empty.");
            }

            // check if the very first symbol of the address pattern is compliant to the standard
            if (addressPattern[0] != OscProtocol.SymbolAddressSeparator)
            {
                throw new ArgumentException("OSC Serializer ERROR: Cannot convert, provided address pattern doesn't begin with a '/'.");
            }

            // + 1 accounts for the "," symbol in the type tag string
            int typeTagBinLength = OscUtil.GetNextMultipleOfFour(arguments.Length + 1);
            int argBinLength = 0;

            // get the binary length of the arg array
            for (int i = 0; i < arguments.Length; i++)
            {
                argBinLength += GetByteLength(arguments[i]);
            }
            // create binary data array of appropriate length
            byte[] data = new byte[addressPattern.OscLength + typeTagBinLength + argBinLength];

            addressPattern.CopyTo(data, 0);

            // set the first character of the type tag string to be the separator
            data[addressPattern.OscLength] = OscProtocol.SymbolComma;

            int currentPos = addressPattern.OscLength + typeTagBinLength;

            for (int i = 0; i < arguments.Length; i++)
            {
                GetBytesArg(arguments[i], out data[addressPattern.OscLength + i + 1]).CopyTo(data, currentPos);
                currentPos += GetByteLength(arguments[i]);
            }

            return data;

        }


        /// <summary>
        /// Creates a byte array of OSC binary data using the provided OSC Message.
        /// </summary>
        /// <param name="message"> OSC Message to be serialized. </param>       
        /// <returns> An OSC Protocol-compliant byte array containing the serialized message. </returns>
        public static byte[] GetBytes(OscMessage message)
        {
            return GetBytesMessage(message.AddressPattern, message.Arguments);
        }

        /// <summary>
        /// Creates a byte array of OSC binary data using the provided OSC Bundle.
        /// </summary>
        /// <param name="bundle"> OSC Bundle to be serialized. </param>
        /// <returns> An OSC Protocol-compliant byte array containing the serialized bundle. </returns>
        public static byte[] GetBytes(OscBundle bundle)
        {
            throw new NotImplementedException();
        }


        public static byte[] GetBytes(OscMessage[] messages)
        {
            throw new NotImplementedException();
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
        /// Creates a binary packet of OSC data that is a bundle, containing all provided packets, timestamped for immediate execution on receipt.  
        /// </summary>
        /// <param name="packets"> OSC binary packets to be bundled up. </param>
        /// <returns> An OSC binary packet containing the bundle. </returns>
        public static OscPacketBytes BundleToBytes(params OscPacketBytes[] packets)
        {
            // set initial length to "#bundle" length + timestamp length
            int length = OscProtocol.BundleDesignator.Length + OscProtocol.DoubleChunk;
            int currentPos = length;


            for (int i = 0; i < packets.Length; i++)
            {
                length += OscProtocol.SingleChunk + packets[i].Length;
            }

            byte[] data = new byte[length];

            OscProtocol.BundleDesignator.CopyTo(data, 0);
            OscSerializer.GetBytes(OscTime.Immediately).CopyTo(data, 8);

            for (int i = 0; i < packets.Length; i++)
            {
                GetBytes(packets[i].Length).CopyTo(data, currentPos);
                currentPos += OscProtocol.SingleChunk;

                packets[i].BinaryData.CopyTo(data, currentPos);
                currentPos += packets[i].Length;
            }

            return new OscPacketBytes(data);

        }

        /// <summary>
        /// Creates a binary packet of OSC data that is a bundle, containing all provided packets, using the provided timestamp.  
        /// </summary>
        /// <param name="timestamp"> The timestamp to stamp the bundle with. </param>
        /// <param name="packets"> OSC binary packets to be bundled up. </param>
        /// <returns> An OSC binary packet containing the bundle. </returns>
        public static OscPacketBytes BundleToBytes(OscTimestamp timestamp, params OscPacketBytes[] packets )
        {
            // set initial length to "#bundle" length + timestamp length
            int length = OscProtocol.BundleDesignator.Length + OscProtocol.DoubleChunk;
            int currentPos = length;


            for (int i = 0; i < packets.Length; i++)
            {
                length += OscProtocol.SingleChunk + packets[i].Length;
            }

            byte[] data = new byte[length];

            OscProtocol.BundleDesignator.CopyTo(data, 0);
            GetBytes(timestamp).CopyTo(data, 8);

            for (int i = 0; i < packets.Length; i++)
            {

                GetBytes(packets[i].Length).CopyTo(data, currentPos);
                currentPos += OscProtocol.SingleChunk;

                packets[i].BinaryData.CopyTo(data, currentPos);
                currentPos += packets[i].Length;

            }

            return new OscPacketBytes(data);

        }

        /// <summary>
        /// Packs the preformatted binary data into an OSC packet and adds the right header. Make sure the data *is* OSC-compliant, that is, use at your own risk.
        /// </summary>
        /// <param name="binaryData"> A byte array containing preformatted, OSC-compliant byte data. hopefully. </param>
        /// <returns> An OSC binary packet containing the bundle. </returns>
        public static OscPacketBytes BundleToBytes(byte[] binaryData)
        {
            byte[] data = new byte[binaryData.Length + OscProtocol.BundleDesignator.Length + OscProtocol.DoubleChunk];

            OscProtocol.BundleDesignator.CopyTo(data, 0);
            GetBytes(OscTime.Immediately).CopyTo(data, 8);
            binaryData.CopyTo(data, 16);

            return new OscPacketBytes(data);

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
            typeTag = OscProtocol.TypeTagInteger;

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
            typeTag = OscProtocol.TypeTagFloat;

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
            typeTag = OscProtocol.TypeTagDouble;

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
        public static byte[] ArgumentToBinary(OscString arg, out byte typeTag)
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
            typeTag = OscProtocol.TypeTagLong;

            return GetBytes(arg);
        }

        #endregion


        #region TIMESTAMP
        /// <summary>
        /// Converts an OSC timestamp into a byte array.
        /// </summary>
        /// <param name="timestamp"> Timestamp to be converted. </param>
        /// <returns> A byte array. </returns>
        public static byte[] GetBytes(OscTimestamp timestamp)
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
        public static byte[] GetBytesArg(OscTimestamp timestamp, out byte typeTag)
        {
            byte[] data = BitConverter.GetBytes(timestamp.NtpTimestamp);

            if (BitConverter.IsLittleEndian)
            {
                OscUtil.SwapEndian(data);
            }

            typeTag = OscProtocol.TypeTagTimestamp;

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
                    return ArgumentToBinary(oscString, out typeTag);
                    
                case byte[] argByte:
                    return GetBytesArg(argByte, out typeTag);

                case bool argBool:
                    return GetBytesArg(argBool, out typeTag);

                case char argChar:
                    return GetBytesArg(argChar.ToString(), out typeTag);

                case OscTimestamp argTimestamp:
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
                    return OscProtocol.SingleChunk;

                case long _:
                    return OscProtocol.DoubleChunk;

                case float _:
                    return OscProtocol.SingleChunk;

                case double _:
                    return OscProtocol.DoubleChunk;

                case string argString:
                    return GetByteLength(argString);

                case OscString oscString:
                    return GetByteLength(oscString);

                case byte[] argByte:
                    return GetByteLength(argByte);

                case bool _:
                    return OscProtocol.SingleChunk;

                case char _:
                    return OscProtocol.SingleChunk;

                case OscTimestamp _:
                    return OscProtocol.DoubleChunk;

                default:
                    throw new ArgumentException("Command Error: Argument " + arg.ToString() + " of unsupported type.");

            }

        }

    }

}
