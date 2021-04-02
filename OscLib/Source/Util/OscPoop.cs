using System;
using System.Collections.Generic;
using System.Text;

namespace OscLib
{
    // random bits and pieces, old methods and such, preserved for possible disassembly and spare part use lol

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
    */
}
