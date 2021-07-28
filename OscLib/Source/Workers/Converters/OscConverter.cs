using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace OscLib
{
    /// <summary>
    /// Thrown when encountering an unsupported OSC Type Tag.
    /// </summary>
    public class OscTypeTagNotSupportedException : Exception
    {
        public OscTypeTagNotSupportedException()
            :base()
        {
        }

        public OscTypeTagNotSupportedException(string message)
            :base(message)
        {
        }

        public OscTypeTagNotSupportedException(string message, Exception inner)
            :base(message, inner)
        {
        }

    }


    /// <summary>
    /// Implements a set methods allowing for converting messages and bundles into OSC byte data and back.  
    /// </summary>
    /// <remarks>
    /// This base class implements methods for converting OSC Messages and Bundles to OSC Packets and vice versa, but implementing the exact methods for serializing and deserializing 
    /// arguments is left to derived classes. Different applications utilising OSC might have slightly different implementations of the OSC protocol -- some don't use double-length 
    /// floats, some use an integer equal to 1 or 0 instead of T and F typetags for sending booleans, some use custom typetags, and so on. Some applications don't even allow any arguments
    /// apart from the bare standard int32, float32, OSC-string and OSC-blob. All this can be accounted for in method overloads in the derived classes. 
    /// </remarks>
    public abstract class OscConverter
    {
        // array-related type tags
        protected const byte _arrayOpen = (byte)'[';
        protected const byte _arrayClose = (byte)']';

        /// <summary> Controls whether this version of OSC Protocol demands adding an empty type tag string (that is, a comma followed by three null bytes) to a message that doesn't contain any arguments. </summary>
        protected bool _addEmptyTypeTagStrings;

        /// <summary> Controls whether this version of OSC Protocol supports argument arrays, designated by square bracket symbols in the type tag string. </summary>
        /// <remarks> 
        /// If set to "False", any arrays found in outgoing messages' arguments will be "flattened" - that is, all arguments contained 
        /// inside arrays will be added linearly. Argument arrays in incoming messages will similarly be "flattened" - the arguments themselves stay, 
        /// the encompassing arrays are removed. 
        /// </remarks>
        protected bool _supportsArgumentArrays;

        /// <summary> Controls whether this version of OSC Protocol demands adding an empty type tag string (that is, a comma followed by three null bytes) to a message that doesn't contain any arguments. </summary>
        public virtual bool AddEmptyTypeTagStrings { get => _addEmptyTypeTagStrings; set => _addEmptyTypeTagStrings = value; }

        /// <summary> Controls whether this version of OSC Protocol supports argument arrays, designated by square bracket symbols in the type tag string. </summary>
        /// <remarks> 
        /// If set to "False", any arrays found in outgoing messages' arguments will be "flattened" - that is, all arguments contained 
        /// inside arrays will be added linearly. Argument arrays in incoming messages will similarly be "flattened" - the arguments themselves stay, 
        /// the encompassing arrays are removed. 
        /// </remarks>
        public virtual bool SupportsArgumentArrays { get => _supportsArgumentArrays; set => _supportsArgumentArrays = value; }


        #region ADDING BYTES OF WHOLE MESSAGES / BUNDLES
        /// <summary>
        /// Serializes the provided OSC Message into byte data, adds it to the provided byte array.
        /// </summary>
        /// <param name="message"> The message to convert to bytes. </param>
        /// <param name="array"> The byte array to add the message data to. </param>
        /// <param name="extPointer"> The external pointer designating the index from which to add data to array. Will be shifted forwards by the length of added data. </param>
        /// <exception cref="ArgumentException"> Thrown if the provided data array is too small to fit the message into. </exception>
        protected void AddMessageAsBytes(OscMessage message, byte[] array, ref int extPointer)
        {
            int msgStart = extPointer;
            int tagPointer = msgStart + message.AddressPattern.OscSize;

            message.AddressPattern.CopyContentsToArray(array, msgStart);

            if (message.ArgumentsCount > 0)
            {
                // add the address pattern comma
                array[tagPointer] = OscProtocol.Comma;

                // +1 is for the comma, account for padding null bytes
                int typeTagLength = GetTypeTagStringLength(message.GetArguments()) + 1;
                extPointer = tagPointer + typeTagLength.NextX4();

                // move the tag pointer 1 byte forward
                tagPointer++;

                AddArgArrayAsBytes(message.GetArguments(), array, ref extPointer, ref tagPointer);

            }
            else
            {
                if (_addEmptyTypeTagStrings)
                {
                    // add the address pattern comma
                    array[tagPointer] = OscProtocol.Comma;

                    // shift pointer forward by 4 bytes - the comma plus three null bytes
                    extPointer = tagPointer + OscProtocol.Chunk32;
                }

            }
         
        }


        /// <summary>
        /// Serializes the provided OSC Bundle into byte data, adds it to the provided byte array.
        /// </summary>
        /// <param name="bundle"> The bundle to convert to bytes. </param>
        /// <param name="array"> The byte array to add bundle data to. </param>
        /// <param name="extPointer"> The external pointer designating the index from which to add data to array. Will be shifted forwards by the length of added data. </param>
        protected void AddBundleAsBytes(OscBundle bundle, byte[] array, ref int extPointer)
        {
            int bndStart = extPointer;
            extPointer += OscBundle.BundleHeaderLength;

            AddBundleHeader(array, bndStart, bundle.Timetag);

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

        #endregion // ADDING BYTES OF WHOLE MESSAGES / BUNDLES


        #region ADDING BYTE "CONTENTS"
        /// <summary>
        /// Orders the provided OSC Messages into a byte array of OSC Bundle "contents", adds it to the provided byte array starting from the provided pointer.
        /// </summary>
        /// <param name="messages"> An array of OSC Messages to order. </param>
        /// <param name="array"> The target byte array. </param>
        /// <param name="extPointer"> The data will be added to target array from this index onwards. This pointer will be shifted forwards by the length of added data. </param>
        protected void AddBytesAsContent(OscMessage[] messages, byte[] array, ref int extPointer)
        {

            for (int i = 0; i < messages.Length; i++)
            {
                // leave space for length
                int startPointer = extPointer;
                extPointer += OscProtocol.Chunk32;

                AddMessageAsBytes(messages[i], array, ref extPointer);

                int endPointer = extPointer;

                int length = endPointer - startPointer - OscProtocol.Chunk32;

                // add length
                OscSerializer.AddBytes(length, array, startPointer);

            }

        }


        /// <summary>
        /// Orders the provided OSC Bundles into a byte array of OSC Bundle "contents", adds it to the provided byte array starting from the provided pointer.
        /// </summary>
        /// <param name="bundles"> An array of OSC Bundles to order. </param>
        /// <param name="array"> The target byte array. </param>
        /// <param name="extPointer"> The data will be added to target array from this index onwards. This pointer will be shifted forwards by the length of added data. </param>
        protected void AddBytesAsContent(OscBundle[] bundles, byte[] array, ref int extPointer)
        {

            for (int i = 0; i < bundles.Length; i++)
            {
                // leave space for length
                int startPointer = extPointer;
                extPointer += OscProtocol.Chunk32;

                AddBundleAsBytes(bundles[i], array, ref extPointer);

                int endPointer = extPointer;

                int length = endPointer - startPointer - OscProtocol.Chunk32;

                // add length
                OscSerializer.AddBytes(length, array, startPointer);

            }

        }


        /// <summary>
        /// Orders the provided OSC Packets into a byte array of OSC Bundle "contents", adds it to the provided byte array starting from the provided pointer.
        /// </summary>
        /// <param name="packets"> An array of OSC Packets to order. </param>
        /// <param name="array"> The target byte array. </param>
        /// <param name="extPointer"> The data will be added to target array from this index onwards. This pointer will be shifted forwards by the length of added data. </param>
        protected void AddBytesAsContent(OscPacket[] packets, byte[] array, ref int extPointer)
        {

            for (int i = 0; i < packets.Length; i++)
            {
                OscSerializer.AddBytes(packets[i].Size, array, ref extPointer);

                packets[i].CopyContentsToArray(array, extPointer);
                extPointer += packets[i].Size;
            }

        }

        #endregion // ADDING BYTE "CONTENTS"


        #region PACKET SERIALIZATION
        /// <summary>
        /// Converts the provided OSC Message into bytes and packs them into an OSC Packet. 
        /// </summary>
        /// <param name="message"> The OSC Message to be converted. </param>
        /// <returns></returns>
        public OscPacket GetPacket(OscMessage message)
        {
            byte[] binaryData = new byte[GetMessageOscSize(message)];

            int fakePointer = 0;

            AddMessageAsBytes(message, binaryData, ref fakePointer);

            return new OscPacket(binaryData);
        }


        /// <summary>
        /// Creates an OSC Message out of the provided address pattern and argument array, converts it into bytes and packs them into an OSC Packet.
        /// </summary>
        /// <param name="addressPattern"> The address pattern of the message. </param>
        /// <param name="arguments"> The arguments </param>
        /// <returns></returns>
        public OscPacket GetPacket(OscString addressPattern, object[] arguments = null)
        {
            // create a message out of the provided stuff first
            OscMessage message = new OscMessage(addressPattern, arguments);

            byte[] binaryData = new byte[GetMessageOscSize(message)];

            int fakePointer = 0;

            AddMessageAsBytes(message, binaryData, ref fakePointer);

            return new OscPacket(binaryData);
        }


        public OscPacket GetPacket(OscBundle bundle)
        {
            byte[] binaryData = new byte[GetBundleOscSize(bundle)];

            int fakePointer = 0;

            AddBundleAsBytes(bundle, binaryData, ref fakePointer);

            return new OscPacket(binaryData);
        }


        public OscPacket GetPacket(OscPacket[] packets)
        {
            int length = OscBundle.BundleHeaderLength;

            for (int i = 0; i < packets.Length; i++)
            {
                length += packets[i].Size + OscProtocol.Chunk32;
            }

            byte[] data = new byte[length];

            AddBundleHeader(data, 0);

            int pointer = OscBundle.BundleHeaderLength;

            AddBytesAsContent(packets, data, ref pointer);

            return new OscPacket(data);
        }


        public OscPacket GetPacket(OscPacket[] packets, OscTimetag timetag)
        {
            int length = OscBundle.BundleHeaderLength;

            for (int i = 0; i < packets.Length; i++)
            {
                length += packets[i].Size + OscProtocol.Chunk32;
            }

            byte[] data = new byte[length];

            AddBundleHeader(data, 0, timetag);

            int pointer = OscBundle.BundleHeaderLength;

            AddBytesAsContent(packets, data, ref pointer);

            return new OscPacket(data);
        }


        #endregion // PACKET SERIALIZATION


        #region PACKET DESERIALIZATION
        /// <summary>
        /// Deserializes a single OSC message from the byte array, using an external pointer to navigate it (subject to there being an OSC Message at the pointer's initial position, of course).
        /// </summary>
        /// <param name="data"> The byte array containing the message. </param>
        /// <param name="extPointer"> Points at the start of the message. Will be shifted forwards to the end of the message. </param>
        /// <param name="length"> The length of this message in bytes. </param>
        /// <returns> The resultant OSC message. </returns>
        /// <exception cref="ArgumentException"> Thrown when no message is found at pointer position. </exception>
        /// <exception cref="InvalidOperationException"> Thrown when the message can't be parsed or has errors. </exception>
        public OscMessage GetMessage(byte[] data, ref int extPointer, int length)
        {
            // should actually contain a message
            if (data.CheckOscContents(extPointer) != PacketContents.Message)
            {
                throw new ArgumentException("OSC Deserializer ERROR: No OSC Message found at pointer position, expecting a '/' symbol. Pointer at: " + extPointer);
            }

            OscString addressPattern;

            object[] arguments = null;

            int addressPatternLength = -1, typeTagStart = -1;

            int msgStart = extPointer;

            // find the address pattern
            addressPatternLength = OscUtil.FindLengthOfOscString(data, msgStart);

            // if address pattern's length is unknown at this point, something's gone very wrong
            if (addressPatternLength < 0)
            {
                throw new InvalidOperationException("OSC Converter ERROR: OSC Message address pattern couldn't be parsed. ");
            }
            else
            {
                addressPattern = new OscString(data, msgStart, addressPatternLength);
            }

            // move pointer
            extPointer += addressPattern.OscSize;

            if (extPointer >= msgStart + length)
            {
                // if we've reached the end of a a message already, and we don't expect an empty type tag string, we might as well return it 
                if (!_addEmptyTypeTagStrings)
                {
                    return new OscMessage(addressPattern);
                }
                else
                {
                    throw new ArgumentException("OSC Converter ERROR: Cannot deserialize OSC Packet - expecting a type tag string, found none. ");
                }

            }

            // check if the address string exists at all
            if (data[extPointer] == OscProtocol.Comma)
            {
                typeTagStart = extPointer;

                int typeTagLength = OscUtil.FindLengthOfOscString(data, typeTagStart);

                // shift forwards 
                extPointer += typeTagLength.NextX4();

                // get the arguments
                arguments = BytesToArgArray(data, ref extPointer, ref typeTagStart);

            }
            else
            {
                // TODO: Add something to try and decypher arguments in a typetag-less message
                // for now though, we'll just go ahead and fail :(
                throw new ArgumentException("OSC Converter ERROR: Cannot deserialize OSC Packet - expecting a type tag string, found none. ");
            }

            return new OscMessage(addressPattern, arguments);

        }


        /// <summary>
        /// Deserializes an OSC message from the byte array that only contains that one message.
        /// </summary>
        /// <param name="data"> Byte array containing the OSC message. </param>
        /// <returns> The resultant OSC message. </returns>
        public OscMessage GetMessage(byte[] data)
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
        public OscMessage GetMessage<Packet>(Packet oscPacket) where Packet : IOscPacket
        {
            return GetMessage(oscPacket.GetContents());
        }


        /// <summary>
        /// Deserializes an OSC Bundle from the byte array, using an external pointer to navigate it.
        /// </summary>
        /// <param name="data"> The byte array containing the bundle. </param>
        /// <param name="extPointer"> Points at the start of the bundle. Will be shifted forwards to the end of the bundle. </param>
        /// <param name="length"> The length of this bundle in bytes. </param>
        /// <returns> The resultant OSC Bundle. </returns>
        public OscBundle GetBundle(byte[] data, ref int extPointer, int length)
        {
            int bndStart = extPointer;

            if (data[bndStart] != OscProtocol.BundleMarker)
            {
                throw new ArgumentException("OSC Converter ERROR: Cannot get OSC Bundle, provided byte array is invalid.");
            }

            // let's skip the "#bundle" bit
            extPointer += OscBundle.MarkerStringLength;

            // get timestamp
            OscTimetag timetag = OscDeserializer.GetTimetag(data, ref extPointer);

            // if we're past the length at this, that means the bundle's empty and we can return it
            if (extPointer > bndStart + length)
            {
                return new OscBundle(timetag);
            }

            int contentStart = extPointer;
            int bndTotal = 0, msgTotal = 0, elementLength = 0;

            // get the number of bundles and messages in the elements (not going into messages contained within bundles)
            while (extPointer < bndStart + length)
            {
                if (data[extPointer] == OscProtocol.BundleMarker)
                {
                    // element is a bundle, move ahead by the length of an element
                    bndTotal++;

                    extPointer += elementLength;
                }
                else if (data[extPointer] == OscProtocol.Separator)
                {
                    // element is a message, move ahead by the length of an element
                    msgTotal++;

                    extPointer += elementLength;
                }
                else
                {
                    // we got the length of next element here
                    elementLength = OscDeserializer.GetInt32(data, ref extPointer);
                }

            }

            // move the pointer back
            extPointer = contentStart;

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
            while (extPointer < bndStart + length)
            {
                if (data[extPointer] == OscProtocol.BundleMarker)
                {
                    bundles[bndCount] = GetBundle(data, ref extPointer, elementLength);
                    bndCount++;
                }
                else if (data[extPointer] == OscProtocol.Separator)
                {
                    messages[msgCount] = GetMessage(data, ref extPointer, elementLength);
                    msgCount++;
                }
                else
                {
                    // there should be the length of next element here
                    elementLength = OscDeserializer.GetInt32(data, ref extPointer);
                }

            }

            return new OscBundle(timetag, bundles, messages);

        }


        /// <summary>
        /// Deserializes an OSC Bundle from the byte array.
        /// </summary>
        /// <param name="data"> The byte array containing the bundle. </param>
        /// <returns> The resultant OSC Bundle. </returns>
        public OscBundle GetBundle(byte[] data)
        {
            int pointer = 0;

            return GetBundle(data, ref pointer, data.Length);
        }


        /// <summary>
        /// Creats an OSC Bundle out of the provided OSC binary packet.
        /// </summary>
        /// <typeparam name="Packet"> The packet struct fed into this method should implement the IOscPacket interface.  </typeparam>
        /// <param name="oscPacket"> The OSC Packet containing an OSC Bundle. </param>
        /// <returns></returns>
        public OscBundle GetBundle<Packet>(Packet oscPacket) where Packet : IOscPacket
        {
            return GetBundle(oscPacket.GetContents());
        }


        /// <summary>
        /// Converts an array of OSC binary containing at least one bundle into a "flat" array of OSC Bundles. That is, this method extracts 
        /// all bundles from their encompassing bundles and presents them all as equal-level elements that only contain OSC Messages.
        /// </summary>
        /// <param name="data"> A byte array containing at least one OSC Bundle. </param>
        /// <returns> An array of OSC Bundles. </returns>
        public OscBundle[] GetBundles(byte[] data)
        {
            if (data[0] != OscProtocol.BundleMarker)
            {
                throw new ArgumentException("OSC Deserializer ERROR: Cannot deserialize OSC bundle, provided byte array is invalid.");
            }

            // if bundle has less than one element in it, we can just return it now. This should show up as binaryData being less or equal in length 
            // to the smallest possible bundle length - bundle designator + timestamp. 
            if (data.Length <= OscBundle.BundleHeaderLength)
            {
                // get timestamp
                OscTimetag timetag = OscDeserializer.GetTimetag(data, 8);

                return new OscBundle[1] { new OscBundle(timetag) };

            }

            // get a quick tally of bundles currently present in the data
            int bundlesTotal = 0;
            int pointer = 0;

            while (pointer < data.Length)
            {
                if (data[pointer] == OscProtocol.BundleMarker)
                {
                    bundlesTotal++;

                    // nothing interesting will be contained in the next 16 bytes
                    pointer += OscBundle.BundleHeaderLength;
                }
                else
                {
                    // this should catch the very first address separator in an address string - the others will be jumped over
                    if (data[pointer] == OscProtocol.Separator)
                    {
                        // we got a message, so lets jump over it - there won't be any bundles within it
                        // get length of the message - it should be contained in 4 bytes before the current position
                        pointer += OscDeserializer.GetInt32(data, pointer - OscProtocol.Chunk32);
                    }
                    else
                    {
                        pointer += OscProtocol.Chunk32;
                    }

                }

            }

            // allocate a temporary array for keeping track of layers of bundled bundles
            OscTimetag[] timetagStack = new OscTimetag[bundlesTotal];

            // an array into which we'll be storing all the bundles
            OscBundle[] bundleArray = new OscBundle[bundlesTotal];

            // get past the "#bundle " bit in the beginning
            pointer = 8;

            int currentTimetag = 0;

            // get the first timetag
            timetagStack[currentTimetag] = OscDeserializer.GetTimetag(data, ref pointer);

            int bundleIndex = 0;

            // the recursive horror that will extract the bundles. it shamefully hides itself inside this method, in hope to avoid prying, cruel gazes and unkind words, a sad monster shambling in the dark 
            void GetBundleRecursive(int length, ref int externalPointer, byte[] binaryData, OscBundle[] bndArray, ref int currentBundleIndex, OscTimetag[] timestampStack, ref int currentTimestampIndex)
            {
                int thisBundleIndex = currentBundleIndex;

                int start = externalPointer - OscBundle.BundleHeaderLength;

                // change later to remove the list and use something more memory-friendly
                List<OscMessage> messageList = new List<OscMessage>(8);

                while (externalPointer < start + length)
                {
                    // get a length of an element
                    int elementLength = OscDeserializer.GetInt32(binaryData, ref externalPointer);

                    // check if it's a bundle or a message
                    if (binaryData[externalPointer] == OscProtocol.Separator)
                    {
                        messageList.Add(GetMessage(binaryData, ref externalPointer, elementLength));
                    }
                    else if (binaryData[externalPointer] == OscProtocol.BundleMarker)
                    {
                        // shift external pointer forward
                        externalPointer += OscBundle.MarkerStringLength;

                        // get timestamp
                        OscTimetag timestamp = OscDeserializer.GetTimetag(binaryData, ref externalPointer);

                        // check if the bundle's timestamp is later than the enveloping bundle's timestamp
                        if (timestampStack[currentTimestampIndex].Ticks <= timestamp.Ticks)
                        {
                            currentTimestampIndex++;
                            currentBundleIndex++;

                            timestampStack[currentTimestampIndex] = timestamp;

                            GetBundleRecursive(elementLength, ref externalPointer, binaryData, bndArray, ref currentBundleIndex, timestampStack, ref currentTimestampIndex);

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

                bndArray[thisBundleIndex] = new OscBundle(timestampStack[currentTimestampIndex], messageList.ToArray());

            }

            // start the recursive horror
            GetBundleRecursive(data.Length, ref pointer, data, bundleArray, ref bundleIndex, timetagStack, ref currentTimetag);

            return bundleArray;

        }

        #endregion


        #region GETTING ELEMENT LENGTH
        /// <summary>
        /// Calculates the binary size of the provided OSC Message according to this OSC Protocol implementation's specifics.
        /// </summary>
        /// <param name="message"> The message to measure. </param>
        /// <returns> The length of the provided message in bytes. </returns>
        public int GetMessageOscSize(OscMessage message)
        {
            object[] args = message.GetArguments();

            int size = message.AddressPattern.OscSize;

            if (args.Length > 0)
            {
                size += GetArgArrayOscSize(args);

                // add type tag string length, including the comma and null byte padding
                size += (GetTypeTagStringLength(args) + 1).NextX4();
            }
            else if (_addEmptyTypeTagStrings)
            {
                // add a 4-byte chunk - a comma and three null bytes for padding
                size += OscProtocol.Chunk32;
            }

            return size;
        }


        /// <summary>
        /// Calculates the binary size of the provided OSC Bundle according to this OSC Protocol implementation's specifics.
        /// </summary>
        /// <param name="bundle"> The bundle to measure. </param>
        /// <returns> The length of the provided message in bytes. </returns>
        public int GetBundleOscSize(OscBundle bundle)
        {
            int length = OscBundle.BundleHeaderLength;

            for (int i = 0; i < bundle.Messages.Length; i++)
            {
                length += GetMessageOscSize(bundle.Messages[i]);

                // account for length's bytes as well
                length += OscProtocol.Chunk32;
            }

            for (int i = 0; i < bundle.Bundles.Length; i++)
            {
                length += GetBundleOscSize(bundle.Bundles[i]);

                // account for length's bytes as well
                length += OscProtocol.Chunk32;
            }

            return length;
        }

        #endregion // GETTING ELEMENT LENGTH


        #region ABSTRACT/VIRTUAL METHODS
        // the idea is to have the specific behaviour of these methods be defined in the derived classes - depending on how the target implements OSC Protocol

        /// <summary>
        /// Serializes the provided argument into its OSC byte data form and returns it as an array of bytes, provides the corresponding type tag.
        /// </summary>
        /// <param name="arg"> Argument to be serialized. </param>
        /// <param name="typeTag"> Will return the OSC type tag for the provided argument. </param>
        /// <returns> The byte array containing the argument converted to OSC Protocol-compliant binary. </returns>
        /// <exception cref="ArgumentException"> Thrown when the argument is of an unsupported type. </exception>
        protected virtual byte[] GetArgAsBytes<T>(T arg, out byte typeTag)
        {
            int length = GetArgOscSize(arg);
            int pointer = 0;

            byte[] data = new byte[length];

            AddArgAsBytes(arg, data, ref pointer, out typeTag);

            return data;
        }


        /// <summary>
        /// Serializes the provided argument into its OSC byte data form and adds it into an existing byte array, provides the corresponding type tag.  
        /// </summary>
        /// <param name="arg"> The argument to be serialized. </param>
        /// <param name="array"> The target byte array. </param>
        /// <param name="extPointer"> The index from which to add data. Will be shifted forwards by the length of added data. </param>
        /// <param name="typeTag"> Corresponding OSC type tag. </param>
        /// <exception cref="ArgumentException"> Thrown when the argument is of an unsupported type. </exception>
        protected abstract void AddArgAsBytes<T>(T arg, byte[] array, ref int extPointer, out byte typeTag);


        /// <summary>
        /// Serializes the provided argument array into OSC byte data and adds it to the provided byte array.  
        /// </summary>
        /// <param name="argArray"> </param>
        /// <param name="targetArray"> </param>
        /// <param name="argPointer"> Points to the index from which the argument byte data should be added. Will be shifted forwards accordingly. </param>
        /// <param name="tagPointer"> Points to the index from which the type tag string begins - not counting the comma. </param>
        protected virtual void AddArgArrayAsBytes(object[] argArray, byte[] targetArray, ref int argPointer, ref int tagPointer)
        {
            // should be null, if not, something's gone wrong
            if (targetArray[tagPointer] != 0)
            {
                throw new InvalidOperationException("OscConverter ERROR: Cannot serialize argument array, type tag string doesn't fit into target byte array.");
            }

            for (int i = 0; i < argArray.Length; i++)
            {
                if (argArray[i] is object[] array)
                {
                    if (_supportsArgumentArrays)
                    {
                        targetArray[tagPointer++] = _arrayOpen;
                        AddArgArrayAsBytes(array, targetArray, ref argPointer, ref tagPointer);
                        targetArray[tagPointer++] = _arrayClose;
                    }
                    else
                    {
                        // just add the contents of the array, ignore the brackets
                        AddArgArrayAsBytes(array, targetArray, ref argPointer, ref tagPointer);
                    }
                }
                else
                {
                    AddArgAsBytes(argArray[i], targetArray, ref argPointer, out targetArray[tagPointer++]);
                }

            }

        }


        /// <summary>
        /// Calculates the binary size of the provided argument array according to this OSC Protocol implementation's specifics.
        /// </summary>
        protected virtual int GetArgArrayOscSize(object[] argArray)
        {
            if ((argArray == null) || (argArray.Length == 0))
            {
                return 0;
            }

            int length = 0;

            for (int i = 0; i < argArray.Length; i++)
            {
                if (argArray[i] is object[] internalArray)
                {
                    length += GetArgArrayOscSize(internalArray);
                }
                else
                {
                    length += GetArgOscSize(argArray[i]);
                }

            }

            // should already be a multiple of 4, but just in case
            length = length.ThisOrNextX4();

            return length;
        }


        /// <summary>
        /// Returns the length of an OSC type tag string corresponding to the provided argument array. Includes array brackets if necessary. *Doesn't* include null byte padding or the comma symbol.
        /// </summary>
        /// <param name="argArray"> An array of arguments. </param>
        /// <returns></returns>
        protected virtual int GetTypeTagStringLength(object[] argArray)
        {
            if ((argArray == null) || (argArray.Length == 0))
            {
                return 0;          
            }
                       
            int length = 0;

            for (int i = 0; i < argArray.Length; i++)
            {
                if (argArray[i] is object[] internalArray)
                {
                    // if argument arrays aren't supported, the internal array will be added as a "flat" sequence of arguments; otherwise, we need to accomodate the opening and closing brackets
                    length += GetTypeTagStringLength(internalArray);

                    if (_supportsArgumentArrays)
                    {
                        length += 2;
                    }

                }
                else
                {
                    // just a single type tag-carrying byte per argument
                    length++;
                }

            }

            return length;
        }


        /// <summary>
        /// Calculates the binary size of the provided argument, according to this OSC Protocol implementation's specifics.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="arg"></param>
        protected abstract int GetArgOscSize<T>(T arg);


        /// <summary>
        /// Deserializes the part of provided byte array into an object representing the OSC Message argument, starting from the pointer position and according to the provided type tag. 
        /// Shifts the pointer forward accordingly.
        /// </summary>
        /// <remarks>
        /// Words cannot describe how much I HATE the fact that this returns arguments as object, requiring boxing and thus causing the dreaded GC pressure. But, alas, I'm currently too 
        /// stupid to figure out some clever way to avoid this. There is probably a way to deal with this, but it'll have to wait. 
        /// </remarks>
        /// <param name="array"> The byte array containing OSC byte data. </param>
        /// <param name="extPointer"> The external pointer designating the position from which the relevant data starts. </param>
        /// <param name="typeTag"> The type tag of the argument contained in the data. </param>
        protected abstract object BytesToArg(byte[] array, ref int extPointer, byte typeTag);


        protected virtual object[] BytesToArgArray(byte[] sourceArray, ref int argPointer, ref int tagPointer)
        {
            int tagStart = tagPointer;
            int argStart = argPointer;

            int totalArgs = 0;
            int openBrackets = 0;
            
            // tally up the number of args
            while ((tagPointer < sourceArray.Length) && (sourceArray[tagPointer] != 0))
            {
                if (sourceArray[tagPointer] == OscProtocol.Comma)
                {
                    tagPointer++;
                    continue;
                }

                if (!_supportsArgumentArrays)
                {
                    // don't count brackets, count every type tag as the argument array will be flat
                    if ((sourceArray[tagPointer] != _arrayOpen) && (sourceArray[tagPointer] != _arrayClose))
                    {
                        totalArgs++;
                    }

                    tagPointer++;

                }
                else
                {               
                    if (sourceArray[tagPointer] == _arrayOpen)
                    {
                        openBrackets++;
                    }
                    else if (sourceArray[tagPointer] == _arrayClose)
                    {
                        openBrackets--;
                    }

                    tagPointer++;

                    // don't count anything within brackets
                    if (openBrackets == 0)
                    {
                        totalArgs++;
                    }
                    else if (openBrackets < 0)
                    {
                        // if this method's been started from inside an array, this should stop it at the right position 
                        break;
                    }
                                          
                }

            }

            if (openBrackets > 0)
            {
                throw new ArgumentException("OscConverter ERROR: Cannot convert bytes into arguments, type tag string seems to include unclosed arrays.");
            }

            // and now for filling the argument array
            object[] arguments = new object[totalArgs];

            tagPointer = tagStart;

            int argCounter = 0;

            while (argCounter < totalArgs)
            {
                // move over any commas, just in case
                if (sourceArray[tagPointer] == OscProtocol.Comma)
                {
                    tagPointer++;
                    continue;
                }

                if (!_supportsArgumentArrays)
                {
                    if ((sourceArray[tagPointer] == _arrayOpen) || (sourceArray[tagPointer] == _arrayClose))
                    {
                        // ignore brackets
                        tagPointer++;
                        continue;     
                    }

                    arguments[argCounter] = BytesToArg(sourceArray, ref argPointer, sourceArray[tagPointer]);
                    argCounter++;
                    tagPointer++;
                }
                else
                {
                    if (sourceArray[tagPointer] == _arrayOpen)
                    {
                        tagPointer++;
                        arguments[argCounter] = BytesToArgArray(sourceArray, ref argPointer, ref tagPointer);
                    }
                    else
                    {
                        arguments[argCounter] = BytesToArg(sourceArray, ref argPointer, sourceArray[tagPointer]);

                    }

                    tagPointer++;
                    argCounter++;

                }

            }

            return arguments;

        }


        #endregion // ABSTRACT METHODS


        #region STATIC METHODS
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
            OscBundle.CopyMarkerStringTo(target, index);
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
            OscBundle.CopyMarkerStringTo(target, index);
            OscSerializer.AddBytes(timetag, target, index + OscProtocol.Chunk64);
        }

        #endregion // STATIC METHODS

    }

}
