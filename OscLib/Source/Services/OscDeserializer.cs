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

        #region PACKET DESERIALIZATION

        /// <summary>
        /// Deserializes an OSC message from a byte array, using an external pointer to navigate it.
        /// </summary>
        /// <param name="data"> The byte array containing the message. </param>
        /// <param name="pointer"> Points at the start of the message. Will be shifted forwards to the end of the message. </param>
        /// <param name="length"> The length of this message in bytes. </param>
        /// <returns> The resultant OSC message. </returns>
        /// <exception cref="ArgumentException"> Thrown when no message is found at pointer position. </exception>
        /// <exception cref="InvalidOperationException"> Thrown when the message can't be parsed or has errors. </exception>
        public static OscMessage GetMessage(byte[] data, ref int pointer, int length)
        {
            // should start with an '/'
            if (data[pointer] != OscProtocol.Separator)
            {
                throw new ArgumentException("OSC Deserializer ERROR: No OSC Message found at pointer position, expecting a '/' symbol. Pointer at: " + pointer);
            }

            OscString addressPattern;

            object[] arguments = null;

            int addressPatternLength = -1, typeTagStart = -1;

            int msgStart = pointer;

            // find the address pattern
            while (pointer < msgStart + length)
            {
                // move pointer forward by a chunk
                pointer += OscProtocol.Chunk32;

                // preceding chunk ending in a 0 means the pattern ends somewhere within it, or right at the end of the chunk before it.
                if (data[pointer - 1] == 0)
                {
                    for (int i = pointer - 2; i >= pointer - OscProtocol.Chunk32; i--)
                    {
                        if (data[i] != 0)
                        {
                            addressPatternLength = i - msgStart + 1;
                            break;
                        }

                    }

                    // if not yet found, the pattern's end is the last byte of the chunk behind
                    if (addressPatternLength < 0)
                    {
                        addressPatternLength = pointer - msgStart - OscProtocol.Chunk32;
                    }

                    break;

                }

            }

            // if address pattern's length is unknown at this point, something's gone very wrong
            if (addressPatternLength < 0)
            {
                throw new InvalidOperationException("OSC Deserializer ERROR: OSC Message address pattern couldn't be parsed. ");
            }
            else
            {
                addressPattern = new OscString(data, msgStart, addressPatternLength);
            }

            // check if the address string exists at all
            if (data[pointer] == OscProtocol.Comma)
            {
                typeTagStart = pointer;

                // find the end of it
                while (data[pointer] != 0)
                {
                    pointer++;
                }

                // - 1 is to accomodate for the "," in the beginning
                int typeTagsTotal = pointer - typeTagStart - 1;

                arguments = new object[typeTagsTotal];

                // move the pointer to the next 4-byte point after the end of the type tags
                pointer = OscUtil.GetNextMultipleOfFour(pointer);

                if (typeTagsTotal > 0)
                {
                    // the first element in the array will be the "," type tag separator
                    for (int i = 0; i < typeTagsTotal; i++)
                    {
                        switch (data[typeTagStart + 1 + i])
                        {
                            case OscProtocol.TypeTagInt32:
                                arguments[i] = GetInt32(data, ref pointer);
                                break;

                            case OscProtocol.TypeTagFloat32:
                                arguments[i] = GetFloat32(data, ref pointer);
                                break;

                            case OscProtocol.TypeTagFloat64:
                                arguments[i] = GetFloat64(data, ref pointer);
                                break;

                            case OscProtocol.TypeTagInt64:
                                arguments[i] = GetInt64(data, ref pointer);
                                break;

                            case OscProtocol.TypeTagString:
                                arguments[i] = GetString(data, ref pointer);
                                break;

                            case OscProtocol.TypeTagBlob:
                                arguments[i] = GetBlob(data, ref pointer);
                                break;

                            case OscProtocol.TypeTagTime:
                                arguments[i] = GetTimetag(data, ref pointer);
                                break;

                            case OscProtocol.Comma:
                                break;

                            default:
                                throw new InvalidOperationException("OSC Deserializer ERROR: unsupported Type Tag.");

                        }

                        if (pointer > msgStart + length)
                        {
                            throw new InvalidOperationException("OSC Deserializer ERROR: Pointer went beyond the end of message. ");
                        }

                    }

                }

            }

            return new OscMessage(addressPattern, arguments);

        }

        /// <summary>
        /// Deserializes an OSC message from a byte array that only contains that one message.
        /// </summary>
        /// <param name="data"> Byte array containing the OSC message. </param>
        /// <returns> The resultant OSC message. </returns>
        public static OscMessage GetMessage(byte[] data)
        {
            int pointer = 0;

            return GetMessage(data, ref pointer, data.Length);
        }


        /// <summary>
        /// Creates an OSC Message out of the provided OSC binary packet.
        /// </summary>
        /// <typeparam name="Packet"> The packet struct fed into this method should implement the IOscPacket interface. Obviously, it should contain readable OSC data as well. </typeparam>
        /// <param name="oscPacket"> The packet to be deserialized. </param>
        /// <returns> The resultant OSC message. </returns>
        /// <remarks> The method is generic to avoid the struct-as-interface boxing/unboxing shenanigans. </remarks>
        public static OscMessage GetMessage<Packet>(Packet oscPacket) where Packet : IOscPacket
        {
            if (oscPacket.BinaryData[0] != OscProtocol.Separator)
            {
                throw new ArgumentException("OSC Deserializer ERROR: Cannot deserialize OSC message, provided OSC Packet is invalid.");
            }

            return GetMessage(oscPacket.BinaryData);

        }


        public static OscBundle GetBundle(byte[] data, ref int pointer, int length)
        {
            int bndStart = pointer;

            if (data[bndStart] != OscProtocol.BundleMarker)
            {
                throw new ArgumentException("OSC Deserializer ERROR: Cannot deserialize OSC bundle, provided byte array is invalid.");
            }

            // let's skip the "#bundle" bit
            pointer += OscProtocol.BundleStringLength;

            // get timestamp
            OscTimetag timetag = GetTimetag(data, ref pointer);

            // if we're past the length at this, that means the bundle's empty and we can return it
            if (pointer > bndStart + length)
            {
                return new OscBundle(timetag);
            }

            int contentStart = pointer;
            int bndTotal = 0, msgTotal = 0, elementLength = 0;

            // get the number of bundles and messages in the elements (not going into messages contained within bundles)
            while (pointer < bndStart + length)
            {
                if (data[pointer] == OscProtocol.BundleMarker)
                {
                    // element is a bundle, move ahead by the length of an element
                    bndTotal++;

                    pointer += elementLength;
                }
                else if (data[pointer] == OscProtocol.Separator)
                {
                    // element is a message, move ahead by the length of an element
                    msgTotal++;

                    pointer += elementLength;
                }
                else
                {
                    // we got the length of next element here
                    elementLength = GetInt32(data, ref pointer);
                }

            }

            // move the pointer back
            pointer = contentStart;

            // create arrays to contain bundles and messages;
            OscBundle[] bundles = null;
            OscMessage[] messages = null;

            if (bndTotal > 0)
            {
                bundles = new OscBundle[bndTotal];
            }

            if (msgTotal > 0)
            {
                messages = new OscMessage[msgTotal];
            }

            int bndCount = 0, msgCount = 0;

            // go again lol
            while (pointer < bndStart + length)
            {
                if (data[pointer] == OscProtocol.BundleMarker)
                {
                    bundles[bndCount] = GetBundle(data, ref pointer, elementLength);
                    bndCount++;
                }
                else if (data[pointer] == OscProtocol.Separator)
                {
                    messages[msgCount] = GetMessage(data, ref pointer, elementLength);
                    msgCount++;
                }
                else
                {
                    // there should be the length of next element here
                    elementLength = GetInt32(data, ref pointer);
                }

            }

            return new OscBundle(timetag, bundles, messages);

        }


        public static OscBundle GetBundle(byte[] data)
        {
            int pointer = 0;

            return GetBundle(data, ref pointer, data.Length);
        }


        public static OscBundle GetBundle<Packet>(Packet oscPacket) where Packet : IOscPacket
        {
            if (oscPacket.BinaryData[0] != OscProtocol.BundleMarker)
            {
                throw new ArgumentException("OSC Deserializer ERROR: Cannot deserialize OSC message, provided OSC Packet is invalid.");
            }

            return GetBundle(oscPacket.BinaryData);
        }

        #endregion



        #region GET ARGUMENTS (WITH EXTERNAL POINTER)
        // methods for decoding arguments

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

        #endregion

    }

}




