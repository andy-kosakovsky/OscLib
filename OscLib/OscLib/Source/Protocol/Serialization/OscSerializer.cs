using System;
using System.Collections.Generic;
using System.Text;

namespace OscLib
{
    /// <summary>
    /// Translates OSC messages and bundles into binary packets, serializing it to be sent over the network.
    /// </summary>
    public static class OscSerializer
    {
        /// <summary>
        /// Creates a binary packet of OSC data using the provided address string and arguments.
        /// </summary>
        /// <param name="address"> The address string (can be a full address or a pattern) </param>
        /// <param name="arguments"> The arguments (should be of supported types, obviously) </param>
        /// <returns> An OSC-compliant binary packet containing the message. </returns>
        public static OscPacketBinary MessageToBinary(OscString address, params object[] arguments)
        {
            // + 1 accounts for the "," symbol in the type tag string
            int typeTagBinLength = OscUtil.GetNextMultipleOfFour(arguments.Length + 1);
            int argBinLength = 0;

            // get the binary length of the arg array
            for (int i = 0; i < arguments.Length; i++)
            {
                argBinLength += GetArgumentLength(arguments[i]);
            }
            // create binary data array of appropriate length
            byte[] data = new byte[address.OscLength + typeTagBinLength + argBinLength];

            address.CopyTo(data, 0);

            // set the first character of the type tag string to be the separator
            data[address.OscLength] = OscProtocol.SymbolComma;

            int currentPos = address.OscLength + typeTagBinLength;

            for (int i = 0; i < arguments.Length; i++)
            {
                ArgumentToBinary(arguments[i], out data[address.OscLength + i + 1]).CopyTo(data, currentPos);
                currentPos += GetArgumentLength(arguments[i]);
            }

            return new OscPacketBinary(data);

        }

        /// <summary>
        /// Creates a binary packet of OSC data that is a bundle, containing all provided packets, timestamped for immediate execution on receipt.  
        /// </summary>
        /// <param name="packets"> OSC binary packets to be bundled up. </param>
        /// <returns> An OSC binary packet containing the bundle. </returns>
        public static OscPacketBinary BundleToBinary(params OscPacketBinary[] packets)
        {
            // set initial length to "#bundle" length + timestamp length
            int length = OscProtocol.BundleDesignator.Length + OscProtocol.SingleChunk;
            int currentPos = length;


            for (int i = 0; i < packets.Length; i++)
            {
                length += OscProtocol.SingleChunk + packets[i].Length;
            }

            byte[] data = new byte[length];

            OscProtocol.BundleDesignator.CopyTo(data, 0);
            OscSerializer.TimestampToBinary(OscTime.Immediately).CopyTo(data, 8);

            for (int i = 0; i < packets.Length; i++)
            {
                ArgumentToBinary(packets[i].Length).CopyTo(data, currentPos);
                currentPos += OscProtocol.SingleChunk;

                packets[i].BinaryData.CopyTo(data, currentPos);
                currentPos += packets[i].Length;
            }

            return new OscPacketBinary(data);

        }

        /// <summary>
        /// Creates a binary packet of OSC data that is a bundle, containing all provided packets, using the provided timestamp.  
        /// </summary>
        /// <param name="timestamp"> The timestamp to stamp the bundle with. </param>
        /// <param name="packets"> OSC binary packets to be bundled up. </param>
        /// <returns> An OSC binary packet containing the bundle. </returns>
        public static OscPacketBinary BundleToBinary(OscTimestamp timestamp, params OscPacketBinary[] packets )
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
            TimestampToBinary(timestamp).CopyTo(data, 8);

            for (int i = 0; i < packets.Length; i++)
            {

                ArgumentToBinary(packets[i].Length).CopyTo(data, currentPos);
                currentPos += OscProtocol.SingleChunk;

                packets[i].BinaryData.CopyTo(data, currentPos);
                currentPos += packets[i].Length;

            }

            return new OscPacketBinary(data);

        }

        /// <summary>
        /// Packs the preformatted binary data into an OSC packet and adds the right header. Make sure the data *is* OSC-compliant, that is, use at your own risk.
        /// </summary>
        /// <param name="binaryData"> The preformatted, OSC-compliant binary data, hopefully. </param>
        /// <returns> An OSC binary packet containing the bundle. </returns>
        public static OscPacketBinary BundleToBinary(byte[] binaryData)
        {
            byte[] data = new byte[binaryData.Length + OscProtocol.BundleDesignator.Length + OscProtocol.DoubleChunk];

            OscProtocol.BundleDesignator.CopyTo(data, 0);
            TimestampToBinary(OscTime.Immediately).CopyTo(data, 8);
            binaryData.CopyTo(data, 16);

            return new OscPacketBinary(data);

        }


        #region INT32
        /// <summary>
        /// Converts an integer into a byte array, swapping its endianness in the process.
        /// </summary>
        /// <param name="arg"> Integer to be converted. </param>
        /// <returns> A byte array. </returns>
        public static byte[] ArgumentToBinary(int arg)
        {
            byte[] data = BitConverter.GetBytes(arg);

            if (BitConverter.IsLittleEndian)
            {
                OscUtil.SwapEndian(data);
            }

            return data;
        }

        /// <summary>
        /// Converts an integer into a byte array, swapping its endianness in the process, and returns the OSC type tag.
        /// </summary>
        /// <param name="arg"> Integer to be converted. </param>
        /// <param name="typeTag"> Corresponding OSC type tag. </param>
        /// <returns> A byte array. </returns>
        public static byte[] ArgumentToBinary(int arg, out byte typeTag)
        {         
            typeTag = OscProtocol.TypeTagInteger;

            return ArgumentToBinary(arg);
        }

        #endregion


        #region FLOAT32

        /// <summary>
        /// Converts a float into a byte array, swapping its endianness in the process.
        /// </summary>
        /// <param name="arg"> Float to be converted. </param>
        /// <returns> A byte array. </returns>
        public static byte[] ArgumentToBinary(float arg)
        {
            byte[] data = BitConverter.GetBytes(arg);

            if (BitConverter.IsLittleEndian)
            {
                OscUtil.SwapEndian(data);
            }

            return data;
        }

        /// <summary>
        /// Converts a float into a byte array, swapping its endianness in the process, and returns the OSC type tag.
        /// </summary>
        /// <param name="arg"> Float to be converted. </param>
        /// <param name="typeTag"> Corresponding OSC type tag. </param>
        /// <returns> A byte array. </returns>
        public static byte[] ArgumentToBinary(float arg, out byte typeTag)
        {
            typeTag = OscProtocol.TypeTagFloat;

            return ArgumentToBinary(arg);
        }

        #endregion


        #region FLOAT64

        /// <summary>
        /// Converts a double into a byte array, swapping its endianness in the process.
        /// </summary>
        /// <param name="arg"> Double to be converted. </param>
        /// <returns> A byte array. </returns>
        public static byte[] ArgumentToBinary(double arg)
        {
            byte[] data = BitConverter.GetBytes(arg);

            if (BitConverter.IsLittleEndian)
            {
                OscUtil.SwapEndian(data);
            }

            return data;
        }

        /// <summary>
        /// Converts a double into a byte array, swapping its endianness in the process, and returns the OSC type tag.
        /// </summary>
        /// <param name="arg"> Double to be converted. </param>
        /// <param name="typeTag"> Corresponding OSC type tag. </param>
        /// <returns> A byte array. </returns>
        public static byte[] ArgumentToBinary(double arg, out byte typeTag)
        {
            typeTag = OscProtocol.TypeTagDouble;

            return ArgumentToBinary(arg);
        }

        #endregion


        #region STRING

        /// <summary>
        /// Converts a string into an ASCII byte array.
        /// </summary>
        /// <param name="arg"> String to be converted. </param>
        /// <returns> A byte array. </returns>
        public static byte[] ArgumentToBinary(string arg)
        {
            OscString oscString = arg;
            
            return ArgumentToBinary(oscString);
        }

        /// <summary>
        /// Converts a string into an ASCII byte array.
        /// </summary>
        /// <param name="arg"> String to be converted. </param>
        /// <param name="typeTag"> Corresponding OSC type tag. </param>
        /// <returns> A byte array. </returns>
        public static byte[] ArgumentToBinary(string arg, out byte typeTag)
        {
            typeTag = OscProtocol.TypeTagString;

            return ArgumentToBinary(arg);
        }

        /// <summary>
        /// Returns the OSC protocol-compliant byte length of the string.
        /// </summary>
        /// <param name="arg"> String to be measured. </param>
        /// <returns> OSC length of the string. </returns>
        public static int GetArgumentLength(string arg)
        {
            return OscUtil.GetNextMultipleOfFour(arg.Length);
        }

        #endregion


        #region OSC STRING

        /// <summary>
        /// Converts a string into an ASCII byte array.
        /// </summary>
        /// <param name="arg"> String to be converted. </param>
        /// <returns> A byte array. </returns>
        public static byte[] ArgumentToBinary(OscString arg)
        {
            return arg.OscBytes;
        }

        /// <summary>
        /// Converts a string into an ASCII byte array, and returns the OSC type tag.
        /// </summary>
        /// <param name="arg"> String to be converted. </param>
        /// <param name="typeTag"> Corresponding OSC type tag. </param>
        /// <returns> A byte array. </returns>
        public static byte[] ArgumentToBinary(OscString arg, out byte typeTag)
        {
            typeTag = OscProtocol.TypeTagString;

            return ArgumentToBinary(arg);

        }

        /// <summary>
        /// Returns the OSC protocol-compliant byte length of the string.
        /// </summary>
        /// <param name="arg"> String to be measured. </param>
        /// <returns> OSC length of the string. </returns>
        public static int GetArgumentLength(OscString arg)
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
        public static byte[] ArgumentToBinary(byte[] arg)
        {
            byte[] resultArray = new byte[GetArgumentLength(arg)];

            // copy the length into the array
            ArgumentToBinary(arg.Length).CopyTo(resultArray, 0);

            arg.CopyTo(resultArray, 4);

            return resultArray;
        }

        /// <summary>
        /// Converts a byte array into a OSC protocol-compliant binary blob, and returns the type tag.
        /// </summary>
        /// <param name="arg"> Byte array to be converted. </param>
        /// <param name="typeTag"> Corresponding OSC type tag. </param>
        /// <returns> A binary blob - still a byte array but correctly formatted. </returns>
        public static byte[] ArgumentToBinary(byte[] arg, out byte typeTag)
        {
            typeTag = OscProtocol.TypeTagBlob;

            return ArgumentToBinary(arg);
        }

        /// <summary>
        /// Returns the OSC protocol-compliant length of the byte array.
        /// </summary>
        /// <param name="arg"> Byte array to be measured. </param>
        /// <returns> OSC length of the array. </returns>
        public static int GetArgumentLength(byte[] arg)
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
        public static byte[] ArgumentToBinary(bool arg)
        {
            if (arg)
                return ArgumentToBinary(1);
            else
                return ArgumentToBinary(0);
        }

        /// <summary>
        /// Converts a bool into byte representations of 1 if true and 0 if false, and returns the type tag.
        /// </summary>
        /// <param name="arg"> Bool to be converted. </param>
        /// <param name="typeTag"> Corresponding OSC type tag. </param>
        /// <returns> A byte array. </returns>
        public static byte[] ArgumentToBinary(bool arg, out byte typeTag)
        {
            if (arg)
                return ArgumentToBinary(1, out typeTag);
            else
                return ArgumentToBinary(0, out typeTag);
        }

        #endregion


        #region INT64

        /// <summary>
        /// Converts a longint into a byte array, swapping its endianness in the process.
        /// </summary>
        /// <param name="arg"> Longint to be converted. </param>
        /// <returns> A byte array. </returns>
        public static byte[] ArgumentToBinary(long arg)
        {
            byte[] data = BitConverter.GetBytes(arg);

            if (BitConverter.IsLittleEndian)
            {
                OscUtil.SwapEndian(data);
            }

            return data;
        }

        /// <summary>
        /// Converts an longint into a byte array, swapping its endianness in the process, and returns the OSC type tag.
        /// </summary>
        /// <param name="arg"> Longint to be converted. </param>
        /// <param name="typeTag"> Corresponding OSC type tag. </param>
        /// <returns> A byte array. </returns>
        public static byte[] ArgumentToBinary(long arg, out byte typeTag)
        {
            typeTag = OscProtocol.TypeTagLong;

            return ArgumentToBinary(arg);
        }

        #endregion


        #region TIMESTAMP
        /// <summary>
        /// Converts an OSC timestamp into a byte array.
        /// </summary>
        /// <param name="timestamp"> Timestamp to be converted. </param>
        /// <returns> A byte array. </returns>
        public static byte[] TimestampToBinary(OscTimestamp timestamp)
        {
            byte[] data = BitConverter.GetBytes(timestamp.NtpTimestamp);

            if (BitConverter.IsLittleEndian)
            {
                OscUtil.SwapEndian(data);
            }

            return data;
        }

        /// <summary>
        /// Converts an OSC timestamp into a byte array, and returns the OSC type tag.
        /// </summary>
        /// <param name="timestamp"> Timestamp to be converted. </param>
        /// <param name="typeTag"> Corresponding OSC type tag. </param>
        /// <returns> A byte array. </returns>
        public static byte[] TimestampToBinary(OscTimestamp timestamp, out byte typeTag)
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
        /// <param name="arg"> Argument to be serialized. </param>
        /// <param name="typeTag"> Will return the OSC type tag for the provided argument. </param>
        /// <returns> The byte array containing the argument. </returns>
        /// <exception cref="ArgumentException"> Thrown when the argument is of an unsupported type. </exception>
        public static byte[] ArgumentToBinary<T>(T arg, out byte typeTag)
        {
            switch (arg)
            {
                case int argInt:
                    return ArgumentToBinary(argInt, out typeTag);

                case long argLong:
                    return ArgumentToBinary(argLong, out typeTag);

                case float argFloat:
                    return ArgumentToBinary(argFloat, out typeTag);

                case double argDouble:
                    return ArgumentToBinary(argDouble, out typeTag);

                case string argString:
                    return ArgumentToBinary(argString, out typeTag);

                case OscString oscString:
                    return ArgumentToBinary(oscString, out typeTag);
                    
                case byte[] argByte:
                    return ArgumentToBinary(argByte, out typeTag);

                case bool argBool:
                    return ArgumentToBinary(argBool, out typeTag);

                case char argChar:
                    return ArgumentToBinary(argChar.ToString(), out typeTag);

                case OscTimestamp argTimestamp:
                    return TimestampToBinary(argTimestamp, out typeTag);

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
        public static int GetArgumentLength<T>(T arg)
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
                    return GetArgumentLength(argString);

                case OscString oscString:
                    return GetArgumentLength(oscString);

                case byte[] argByte:
                    return GetArgumentLength(argByte);

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
