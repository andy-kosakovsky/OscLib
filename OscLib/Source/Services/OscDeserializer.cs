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
        // temp byte arrays for holding chunks and avoiding gc messes
        // TODO: temp byte arrays in OscDeserializer need to be changed to something that is safe to use with concurrency. Currently that's not the case, which leads to (occasional and rare, but still) data mangling.
        private static byte[] _tempByteArray32 = new byte[4];

        private static byte[] _tempByteArray64 = new byte[8];


        #region PACKET DESERIALIZATION

        /// <summary>
        /// Deserializes an OSC message from a byte array, using an external pointer to navigate it.
        /// </summary>
        /// <param name="pointer"> Used to go through the byte array. </param>
        /// <param name="binaryData"> The byte array containing the message. </param>
        /// <returns> The resultant OSC message. </returns>
        /// <exception cref="ArgumentException"> Thrown when encountering an unsupported OSC type tag. </exception>
        public static OscMessage GetMessage(ref int pointer, byte[] binaryData)
        {
            // should start with an '/'
            if (binaryData[pointer] != OscProtocol.Separator)
            {
                throw new ArgumentException("OSC Deserializer ERROR: OSC Message can't be parsed from pointer position " + pointer);
            }

            OscString addressPattern;
            object[] arguments = null;

            int addressPatternLength = -1, typeTagStart = -1;

            int messageStart = pointer;

            // find the address pattern
            while (pointer < binaryData.Length)
            {
                // move pointer forward by a chunk
                pointer += OscProtocol.Chunk32;

                // preceding chunk ending in a 0 means the pattern ends somewhere within it, or right at the end of the chunk before it.
                if (binaryData[pointer - 1] == 0)
                {
                    for (int i = pointer - 2; i >= pointer - OscProtocol.Chunk32; i--)
                    {
                        if (binaryData[i] != 0)
                        {
                            addressPatternLength = i - messageStart + 1;
                            break;
                        }

                    }

                    // if not yet found, the pattern's end is the last byte of the second chunk behind
                    if (addressPatternLength < 0)
                    {
                        addressPatternLength = pointer - messageStart - OscProtocol.Chunk32;
                    }

                    break;

                }

            }

            // if pattern's length is unknown at this point, something's gone very wrong
            if (addressPatternLength < 0)
            {
                throw new ArgumentException("OSC Deserializer ERROR: OSC Message couldn't be parsed from pointer position " + messageStart);
            }
            else
            {
                addressPattern = new OscString(binaryData, messageStart, addressPatternLength); 
            }

            // check if the address string exists at all
            if (binaryData[pointer] == OscProtocol.Comma)
            {
                typeTagStart = pointer;

                // find the end of it
                while (binaryData[pointer] != 0)
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
                        switch (binaryData[typeTagStart + 1 + i])
                        {
                            case OscProtocol.TypeTagInt32:
                                arguments[i] = GetInt32(ref pointer, binaryData);
                                break;

                            case OscProtocol.TypeTagFloat32:
                                arguments[i] = GetFloat32(ref pointer, binaryData);
                                break;

                            case OscProtocol.TypeTagFloat64:
                                arguments[i] = GetFloat64(ref pointer, binaryData);
                                break;

                            case OscProtocol.TypeTagInt64:
                                arguments[i] = GetInt64(ref pointer, binaryData);
                                break;

                            case OscProtocol.TypeTagString:
                                arguments[i] = GetString(ref pointer, binaryData);
                                break;

                            case OscProtocol.TypeTagBlob:
                                arguments[i] = GetBlob(ref pointer, binaryData);
                                break;

                            case OscProtocol.TypeTagTime:
                                arguments[i] = GetTimestamp(ref pointer, binaryData);
                                break;

                            case OscProtocol.Comma:
                                break;

                            default:
                                throw new ArgumentException("OSC Deserializer Error: unsupported Type Tag.");

                        }

                    }

                }

            }


            if (arguments != null)
            {
                return new OscMessage(addressPattern, arguments);
            }
            else
            {
                return new OscMessage(addressPattern);
            }
       
        }

        /// <summary>
        /// Deserializes an OSC message from a byte array that only contains that message.
        /// </summary>
        /// <param name="binaryData"> Byte array containing the OSC message. </param>
        /// <returns> The resultant OSC message. </returns>
        public static OscMessage GetMessage(byte[] binaryData)
        {
            int pointer = 0;

            return GetMessage(ref pointer, binaryData); 
        }

        /// <summary>
        /// Creates the OSC messages out of the provided OSC binary packet struct.
        /// </summary>
        /// <typeparam name="Packet"> The packet struct fed into this method should implement the IOscPacketBinary interface. </typeparam>
        /// <param name="oscPacketBinary"> The packet to be deserialized. </param>
        /// <returns> The resultant OSC message. </returns>
        /// <remarks> The method is generic to avoid the struct-as-interface boxing/unboxing shenanigans. </remarks>
        public static OscMessage GetMessage<Packet>(Packet oscPacketBinary) where Packet : IOscPacket
        {
            if (oscPacketBinary.BinaryData[0] != OscProtocol.Separator)
            {
                throw new ArgumentException("OSC Deserializer ERROR: Cannot deserialize OSC message, provided OSC Packet is invalid.");
            }

            return GetMessage(oscPacketBinary.BinaryData);

        }


        /// <summary>
        /// Deserializes an array of bundles from the byte array.
        /// </summary>
        /// <param name="binaryData"> Byte array containing at least one bundle. </param>
        /// <returns> An array of readable OSC bundles. </returns>
        public static OscBundle[] GetBundles(byte[] binaryData)
        {
            if (binaryData[0] != OscProtocol.BundleMarker)
            {
                throw new ArgumentException("OSC Deserializer ERROR: Cannot deserialize OSC bundle, provided byte array is invalid.");
            }

            // if bundle has less than one element in it, we can just return it now. This should show up as binaryData being less or equal in length 
            // to the smallest possible bundle length - bundle designator + timestamp. 
            if (binaryData.Length <= OscBundle.HeaderLength)
            {
                // get timestamp
                OscTimetag timestamp = GetTimestamp(8, binaryData);

                return new OscBundle[1] { new OscBundle(timestamp) };

            }

            // get a quick tally of bundles currently present in the data
            int bundlesTotal = 0;
            int pointer = 0;

            while (pointer < binaryData.Length)
            {
                if (binaryData[pointer] == OscProtocol.BundleMarker)
                {
                    bundlesTotal++;

                    // nothing interesting will be contained in the next 16 bytes
                    pointer += OscBundle.HeaderLength;
                }
                else
                {
                    // this should catch the very first address separator in an address string - the others will be jumped over
                    if (binaryData[pointer] == OscProtocol.Separator)
                    {
                        // we got a message, so lets jump over it - there won't be any bundles within it
                        // get length of the message - it should be contained in 4 bytes before the current position
                        pointer += GetInt32(pointer - OscProtocol.Chunk32, binaryData);
                    }
                    else
                    {
                        pointer += OscProtocol.Chunk32;
                    }

                }

            }

            // allocate a temporary array for keeping track of layers of bundled bundles
           OscTimetag[] timestampStack = new OscTimetag[bundlesTotal];

            // an array into which we'll be storing all the bundles
            OscBundle[] bundleArray = new OscBundle[bundlesTotal];

            // get past the "#bundle " bit in the beginning
            pointer = 8;
            
            int currentTimestamp = 0;

            // get the first timestamp
            timestampStack[currentTimestamp] = GetTimestamp(ref pointer, binaryData);

            int bundleIndex = 0;

            // start the recursive horror
            GetBundle(binaryData.Length, ref pointer, binaryData, ref bundleArray, ref bundleIndex, timestampStack, ref currentTimestamp);

            return bundleArray;

        }

        // evil recursive shit to get all bundles
        private static void GetBundle(int length, ref int externalPointer, byte[] binaryData, ref OscBundle[] bundleArray, ref int currentBundleIndex, OscTimetag[] timestampStack, ref int currentTimestampIndex)
        {
            int thisBundleIndex = currentBundleIndex;

            int start = externalPointer - OscBundle.HeaderLength;

            // change later to remove the list and use something more memory-friendly
            List<OscMessage> messageList = new List<OscMessage>(8);

            while (externalPointer < start + length)
            {
                // get a length of an element
                int elementLength = OscDeserializer.GetInt32(ref externalPointer, binaryData);

                // check if it's a bundle or a message
                if (binaryData[externalPointer] == OscProtocol.Separator)
                {
                    messageList.Add(GetMessage(ref externalPointer, binaryData));
                }
                else if (binaryData[externalPointer] == OscProtocol.BundleMarker)
                {
                    // shift external pointer forward
                    externalPointer += OscProtocol.BundleStringLength;

                    // get timestamp
                    OscTimetag timestamp = GetTimestamp(ref externalPointer, binaryData);
                    
                    // check if the bundle's timestamp is later than the enveloping bundle's timestamp
                    if (timestampStack[currentTimestampIndex].Ticks <= timestamp.Ticks)
                    {
                        currentTimestampIndex++;
                        currentBundleIndex++;

                        timestampStack[currentTimestampIndex] = timestamp;

                        GetBundle(elementLength, ref externalPointer, binaryData, ref bundleArray, ref currentBundleIndex, timestampStack, ref currentTimestampIndex);

                        // go back one timestamp
                        currentTimestampIndex--;
                    }
                    else
                    {
                        // discard the bundle
                        externalPointer += elementLength;
                    }

                }

            }

            bundleArray[thisBundleIndex] = new OscBundle(timestampStack[currentTimestampIndex], messageList.ToArray());

        }

        #endregion


        #region GET ARGUMENTS (WITH EXTERNAL POINTER)
        // methods for decoding arguments

        /// <summary>
        /// Gets an int out of the byte array, using an external pointer.
        /// </summary>
        /// <param name="pointer"> Pointing at the index from which the relevant bytes begin. </param>
        /// <param name="data"> Byte array containing the data. </param>
        /// <returns></returns>
        public static int GetInt32(ref int pointer, byte[] data)
        {
            // get 4 bytes of data
            Array.Copy(data, pointer, _tempByteArray32, 0, 4);

            pointer += 4;

            if (BitConverter.IsLittleEndian)
                OscUtil.SwapEndian(_tempByteArray32);

            return BitConverter.ToInt32(_tempByteArray32, 0);
        }

        /// <summary>
        /// Gets a long out of the byte array, using an external pointer.
        /// </summary>
        /// <param name="pointer"> Pointing at the index from which the relevant bytes begin. </param>
        /// <param name="data"> Byte array containing the data. </param>
        /// <returns></returns>
        public static long GetInt64(ref int pointer, byte[] data)
        {
            Array.Copy(data, pointer, _tempByteArray64, 0, 8);

            pointer += 8;

            if (BitConverter.IsLittleEndian)
                OscUtil.SwapEndian(_tempByteArray64);

            return BitConverter.ToInt64(_tempByteArray64, 0);
        }

        /// <summary>
        /// Gets a float out of the byte array, using an external pointer.
        /// </summary>
        /// <param name="pointer"> Pointing at the index from which the relevant bytes begin. </param>
        /// <param name="data"> Byte array containing the data. </param>
        /// <returns></returns>
        public static float GetFloat32(ref int pointer, byte[] data)
        {
            
            Array.Copy(data, pointer, _tempByteArray32, 0, 4);

            pointer += 4;

            if (BitConverter.IsLittleEndian)
                OscUtil.SwapEndian(_tempByteArray32);

            return BitConverter.ToSingle(_tempByteArray32, 0);

        }

        /// <summary>
        /// Gets a double out of the byte array, using an external pointer.
        /// </summary>
        /// <param name="pointer"> Pointing at the index from which the relevant bytes begin. </param>
        /// <param name="data"> Byte array containing the data. </param>
        /// <returns></returns>
        public static double GetFloat64(ref int pointer, byte[] data)
        {

            Array.Copy(data, pointer, _tempByteArray64, 0, 8);

            pointer += 8;

            if (BitConverter.IsLittleEndian)
                OscUtil.SwapEndian(_tempByteArray64);

            return BitConverter.ToDouble(_tempByteArray64, 0);
        }

        /// <summary>
        /// Gets a binary blob out of the byte array, using an external pointer.
        /// </summary>
        /// <param name="pointer"> Pointing at the index from which the relevant bytes begin. </param>
        /// <param name="data"> Byte array containing the data. </param>
        /// <returns> a byte[]. </returns>
        public static byte[] GetBlob(ref int pointer, byte[] data)
        {
            // get length
            int length = GetInt32(ref pointer, data);

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
        /// <param name="pointer"> Pointing at the index from which the relevant bytes begin. </param>
        /// <param name="data"> Byte array containing the data. </param>
        /// <returns></returns>
        public static string GetString(ref int pointer, byte[] data)
        {
            bool stop = false;

            StringBuilder returnString = new StringBuilder();

            while (!stop)
            {
               
                Array.Copy(data, pointer, _tempByteArray32, 0, 4);
                pointer += OscProtocol.Chunk32;

                for (int i = 0; i < _tempByteArray32.Length; i++)
                {
                    if (_tempByteArray32[i] != 0)
                        returnString.Append((char)_tempByteArray32[i]);
                    else
                        stop = true;
                }

            }

            return returnString.ToString();
        }

        /// <summary>
        /// Gets a timestamp out of the byte array, using an external pointer.
        /// </summary>
        /// <param name="pointer"> Pointing at the index from which the relevant bytes begin. </param>
        /// <param name="data"> Byte array containing the data. </param>
        /// <returns> An OscTimestamp. </returns>
        public static OscTimetag GetTimestamp(ref int pointer, byte[] data)
        {
            // decode the 8-byte timestamp into unsigned longint
            Array.Copy(data, pointer, _tempByteArray64, 0, 8);

            pointer += 8;

            if (BitConverter.IsLittleEndian)
                OscUtil.SwapEndian(_tempByteArray64);

            ulong ntpTimestamp = BitConverter.ToUInt64(_tempByteArray64, 0);

            return new OscTimetag(ntpTimestamp);
        }

        #endregion


        #region GET ARGUMENTS (WITH DIRECT POINTER)

        /// <summary>
        /// Gets an int out of the byte array, using a pointer.
        /// </summary>
        /// <param name="pointer"> Pointing at the index from which the relevant bytes begin. </param>
        /// <param name="data"> Byte array containing the data. </param>
        /// <returns></returns>
        public static int GetInt32(int pointer, byte[] data)
        {
            // get 4 bytes of data
            Array.Copy(data, pointer, _tempByteArray32, 0, 4);

            if (BitConverter.IsLittleEndian)
                OscUtil.SwapEndian(_tempByteArray32);

            return BitConverter.ToInt32(_tempByteArray32, 0);
        }

        /// <summary>
        /// Gets a long out of the byte array, using a pointer.
        /// </summary>
        /// <param name="pointer"> Pointing at the index from which the relevant bytes begin. </param>
        /// <param name="data"> Byte array containing the data. </param>
        /// <returns></returns>
        public static long GetInt64(int pointer, byte[] data)
        {
            Array.Copy(data, pointer, _tempByteArray64, 0, 8);

            if (BitConverter.IsLittleEndian)
                OscUtil.SwapEndian(_tempByteArray64);

            return BitConverter.ToInt64(_tempByteArray64, 0);
        }

        /// <summary>
        /// Gets a float out of the byte array, using a pointer.
        /// </summary>
        /// <param name="pointer"> Pointing at the index from which the relevant bytes begin. </param>
        /// <param name="data"> Byte array containing the data. </param>
        /// <returns></returns>
        public static float GetFloat32(int pointer, byte[] data)
        { 
            Array.Copy(data, pointer, _tempByteArray32, 0, 4);

            if (BitConverter.IsLittleEndian)
                OscUtil.SwapEndian(_tempByteArray32);

            return BitConverter.ToSingle(_tempByteArray32, 0);

        }

        /// <summary>
        /// Gets a double out of the byte array, using a pointer.
        /// </summary>
        /// <param name="pointer"> Pointing at the index from which the relevant bytes begin. </param>
        /// <param name="data"> Byte array containing the data. </param>
        /// <returns></returns>
        public static double GetFloat64(int pointer, byte[] data)
        {
            Array.Copy(data, pointer, _tempByteArray64, 0, 8);

 
            if (BitConverter.IsLittleEndian)
                OscUtil.SwapEndian(_tempByteArray64);

            return BitConverter.ToDouble(_tempByteArray64, 0);
        }

        /// <summary>
        /// Gets a binary blob out of the byte array, using a pointer.
        /// </summary>
        /// <param name="pointer"> Pointing at the index from which the relevant bytes begin. </param>
        /// <param name="data"> Byte array containing the data. </param>
        /// <returns> A byte[] </returns>
        public static byte[] GetBlob(int pointer, byte[] data)
        {
            // get length
            int length = GetInt32(ref pointer, data);

            byte[] resultArray = new byte[length];

            // get result
            Array.Copy(data, pointer, resultArray, 0, length);

            // shift pointer (by length + a few empty bytes at the end)
            pointer += OscUtil.GetNextMultipleOfFour(length);

            return resultArray;

        }
        /// <summary>
        /// Gets a string out of the byte array, using a pointer.
        /// </summary>
        /// <param name="pointer"> Pointing at the index from which the relevant bytes begin. </param>
        /// <param name="data"> Byte array containing the data. </param>
        /// <returns></returns>
        public static string GetString(int pointer, byte[] data)
        {
            bool stop = false;

            StringBuilder returnString = new StringBuilder();

            while (!stop)
            {

                Array.Copy(data, pointer, _tempByteArray32, 0, 4);
                pointer += 4;

                for (int i = 0; i < _tempByteArray32.Length; i++)
                {
                    if (_tempByteArray32[i] != 0)
                        returnString.Append((char)_tempByteArray32[i]);
                    else
                        stop = true;
                }

            }

            return returnString.ToString();
        }

        /// <summary>
        /// Gets a timestamp out of the byte array, using a pointer.
        /// </summary>
        /// <param name="pointer"> Pointing at the index from which the relevant bytes begin. </param>
        /// <param name="data"> Byte array containing the data. </param>
        /// <returns></returns>
        public static OscTimetag GetTimestamp(int pointer, byte[] data)
        {
            // decode the 8-byte timestamp into unsigned longint
            Array.Copy(data, pointer, _tempByteArray64, 0, 8);

            if (BitConverter.IsLittleEndian)
                OscUtil.SwapEndian(_tempByteArray64);

            ulong ntpTimestamp = BitConverter.ToUInt64(_tempByteArray64, 0);

            return new OscTimetag(ntpTimestamp);
        }


        #endregion

    }

}




