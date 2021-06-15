using System;
using System.Collections.Generic;
using System.Text;

namespace OscLib
{
    // random bits and pieces, old methods and such, preserved for possible disassembly and spare part use lol
    // nothing to see here, move along

    /*
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
                OscTimetag timestamp = GetTimetag(binaryData, 8);

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
                        pointer += GetInt32(binaryData, pointer - OscProtocol.Chunk32);
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
            timestampStack[currentTimestamp] = GetTimetag(binaryData, ref pointer);

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
                int elementLength = GetInt32(binaryData, ref externalPointer);

                // check if it's a bundle or a message
                if (binaryData[externalPointer] == OscProtocol.Separator)
                {
                    messageList.Add(GetMessage(binaryData, ref externalPointer, elementLength));
                }
                else if (binaryData[externalPointer] == OscProtocol.BundleMarker)
                {
                    // shift external pointer forward
                    externalPointer += OscProtocol.BundleStringLength;

                    // get timestamp
                    OscTimetag timestamp = GetTimetag(binaryData, ref externalPointer);
                    
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
    



    /// <summary>
    /// Serializes the provided argument into its OSC byte data form and returns it as an array of bytes, provides the corresponding type tag.
    /// </summary>
    /// <remarks>
    /// Implemented the way it is to minimize boxing.
    /// </remarks>
    /// <param name="arg"> Argument to be serialized. </param>
    /// <param name="typeTag"> Will return the OSC type tag for the provided argument. </param>
    /// <returns> The byte array containing the argument converted to OSC Protocol-compliant binary. </returns>
    /// <exception cref="ArgumentException"> Thrown when the argument is of an unsupported type. </exception>
    protected virtual byte[] GetArgAsBytes<T>(T arg, out byte typeTag)
    {
        switch (arg)
        {
            case int argInt:
                typeTag = _typeTagInt32;
                return OscSerializer.GetBytes(argInt);

            case long argLong:
                typeTag = _typeTagInt64;
                return OscSerializer.GetBytes(argLong);

            case OscTimetag argTimestamp:
                typeTag = _typeTagTimetag;
                return OscSerializer.GetBytes(argTimestamp);

            case float argFloat:
                typeTag = _typeTagFloat32;
                return OscSerializer.GetBytes(argFloat);

            case double argDouble:
                typeTag = _typeTagFloat64;
                return OscSerializer.GetBytes(argDouble);

            case string argString:
                typeTag = _typeTagString;
                return OscSerializer.GetBytes(argString);

            case OscString oscString:
                typeTag = _typeTagString;
                return OscSerializer.GetBytes(oscString);

            case byte[] argByte:
                typeTag = _typeTagBlob;
                return OscSerializer.GetBytes(argByte);

            // deal with bools as args
            case bool argBool:
                if (argBool)
                {
                    typeTag = _typeTagTrue;
                }
                else
                {
                    typeTag = _typeTagFalse;
                }
                return new byte[0];

            case char argChar:
                return GetArgAsBytes(argChar.ToString(), out typeTag);



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
    public static void AddArgAsBytes<T>(T arg, byte[] array, ref int extPointer, out byte typeTag)
    {
        switch (arg)
        {
            case int argInt:
                AddArgAsBytes(argInt, array, ref extPointer, out typeTag);
                break;

            case long argLong:
                AddArgAsBytes(argLong, array, ref extPointer, out typeTag);
                break;

            case float argFloat:
                AddArgAsBytes(argFloat, array, ref extPointer, out typeTag);
                break;

            case double argDouble:
                AddArgAsBytes(argDouble, array, ref extPointer, out typeTag);
                break;

            case string argString:
                AddArgAsBytes(argString, array, ref extPointer, out typeTag);
                break;

            case OscString oscString:
                AddArgAsBytes(oscString, array, ref extPointer, out typeTag);
                break;

            case byte[] argByte:
                AddArgAsBytes(argByte, array, ref extPointer, out typeTag);
                break;

            case bool argBool:
                AddArgAsBytes(argBool, array, ref extPointer, out typeTag);
                break;

            case char argChar:
                AddArgAsBytes(argChar.ToString(), array, ref extPointer, out typeTag);
                break;

            case OscTimetag argTimetag:
                AddArgAsBytes(argTimetag, array, ref extPointer, out typeTag);
                break;

            default:
                throw new ArgumentException("Command Error: Argument " + arg.ToString() + " of unsupported type.");

        }

    }
    */

}
