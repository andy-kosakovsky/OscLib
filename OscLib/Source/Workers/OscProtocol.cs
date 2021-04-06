using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace OscLib
{

    /// <summary>
    /// Implements a set methods allowing for converting messages and bundles into OSC byte data and back.  
    /// </summary>
    /// <remarks>
    /// This base class implements methods for converting OSC Messages and Bundles to OSC Packets and vice versa, but implementing the exact methods for serializing and deserializing 
    /// arguments is left to derived classes. Different applications utilising OSC might have slightly different implementations of the OSC protocol -- some don't use double-length 
    /// floats, some use an integer of 1 and 0 instead of T and F typetags for sending booleans, some use custom typetags, and so on. Some applications don't even allow any arguments
    /// apart from the bare standard int32, float32, OSC-string and OSC-blob. All this can be accounted for in method overloads in the derived classes. 
    /// </remarks>
    public abstract class OscProtocol
    {
        // byte lengths of data chunks used by OSC

        /// <summary> Length in bytes of a single (32 bits/4 bytes long) OSC data chunk. </summary>
        public const int Chunk32 = 4;
        /// <summary> Length in bytes of a double-sized (64 bits/8 bytes long) OSC data chunk. </summary>
        public const int Chunk64 = 8;


        #region PATTERN MATCHING CONSTS

        /// <summary> Stands for any sequence of zero or more characters in pattern matching. </summary>
        public const byte MatchAnySequence = (byte)'*';

        /// <summary> Stands for any single character in pattern matching. </summary>
        public const byte MatchAnyChar = (byte)'?';


        /// <summary> Opens an array of characters in pattern matching. A match will occur if any of the characters within the array corresponds to a single character. </summary>
        public const byte MatchCharArrayOpen = (byte)'[';

        /// <summary> Closes an array of characters in pattern matching. </summary>
        public const byte MatchCharArrayClose = (byte)']';

        /// <summary> "Reverses" the character array, matching it with any symbol *not* present in it.  </summary>
        public const byte MatchNot = (byte)'!';

        /// <summary> A range symbol used inside character arrays. Stands for the entire range of ASCII symbols between the two around it. </summary>
        public const byte MatchRange = (byte)'-';


        /// <summary> Opens an array of strings in pattern matching. A match will occur if any of the strings within the array matches to a sequence of characters.  </summary>
        public const byte MatchStringArrayOpen = (byte)'{';

        /// <summary> Closes an array of strings in pattern matching. </summary>
        public const byte MatchStringArrayClose = (byte)'}';

        #endregion // PATTERN MATCHING CONSTS


        // consts for other special symbols specified in OSC protocol
        #region SPECIAL SYMBOL CONSTS

        /// <summary> Designates the start of an OSC Bundle. </summary>
        public const byte BundleMarker = (byte)'#';

        /// <summary> Separates parts of an address string inside OSC Messages. Always should be present at the start of an address string. </summary>
        public const byte Separator = (byte)'/';

        /// <summary> Designates the beginning of an OSC type tag string inside messages. Separates strings in string arrays when pattern matching. </summary>
        public const byte Comma = (byte)',';

        /// <summary> Personal space. Not allowed in OSC Method or Container names, otherwise insignificant. </summary>
        public const byte Space = (byte)' ';

        #endregion // SPECIAL SYMBOL CONSTS

        // reserved symbols that shouldn't be used in osc method or container names - just to have them all in a nice container
        private static readonly byte[] _addressReservedSymbols = new byte[] { Space, BundleMarker, MatchAnySequence,
            Comma, Separator, MatchAnyChar, MatchNot, MatchRange, MatchCharArrayOpen, MatchCharArrayClose, MatchStringArrayOpen, MatchStringArrayClose };


        /// <summary> Controls whether this version of OSC Protocol demands adding an empty type tag string (that is, a comma followed by three null bytes) when there aren't any arguments. </summary>
        protected bool _settingAddEmptyTypeTagStrings;

        /// <summary> Controls whether this version of OSC Protocol demands adding an empty type tag string (that is, a comma followed by three null bytes) when there aren't any arguments. </summary>
        public virtual bool SettingAddEmptyTypeTagStrings { get => _settingAddEmptyTypeTagStrings; }



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
            int addrLength = message.AddressPattern.OscLength;

            int msgStart = extPointer;

            int msgLength = GetMessageOscLength(message);

            if (msgStart + msgLength > array.Length)
            {
                throw new ArgumentException("OSC Serializer ERROR: Cannot add OSC data to byte array, array is too short. ");
            }

            message.AddressPattern.CopyTo(array, msgStart);

            extPointer += addrLength;
        
            if (message.Arguments.Length > 0)
            {
                // add the address pattern comma
                array[msgStart + addrLength] = Comma;

                // find the length of type tag, " + 1" accounts for the comma
                int typeTagLength = OscUtil.GetNextMultipleOfFour(message.Arguments.Length + 1);

                extPointer = msgStart + addrLength + typeTagLength;

                for (int i = 0; i < message.Arguments.Length; i++)
                {
                    AddArgAsBytes(message.Arguments[i], array, ref extPointer, out array[msgStart + addrLength + 1 + i]);
                }

            }
            else
            {
                if (_settingAddEmptyTypeTagStrings)
                {
                    // add the address pattern comma
                    array[msgStart + addrLength] = Comma;
                }

                // shift the pointer forwards to the end of the message
                extPointer = msgStart + msgLength;
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
        protected void AddMessageAsBytes(OscString addressPattern, object[] arguments, byte[] array, ref int extPointer)
        {
            // make a new message to check length, etc. This shouldn't be too problematic memory-wise, as we're not creating anything new on the heap
            OscMessage message = new OscMessage(addressPattern, arguments);

            AddMessageAsBytes(message, array, ref extPointer);

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

            int bndLength = GetBundleOscLength(bundle);

            if (bndStart + bndLength > array.Length)
            {
                throw new ArgumentException("OSC Serializer ERROR: Cannot add OSC data to byte array, array is too short. ");
            }

            AddBundleHeader(array, extPointer, bundle.Timetag);

            extPointer += OscBundle.BundleHeaderLength;

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



        #region GETTING BYTES OF WHOLE MESSAGES / BUNDLES

        /// <summary>
        /// Serializes the provided OSC Message into a byte array.
        /// </summary>
        /// <param name="message"> OSC Message to be serialized. </param>       
        /// <returns> An OSC Protocol-compliant byte array containing the serialized message. </returns>
        protected byte[] GetMessageAsBytes(OscMessage message)
        {
            byte[] binaryData = new byte[GetMessageOscLength(message)];

            int pointer = 0;

            AddMessageAsBytes(message, binaryData, ref pointer);

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
        protected byte[] GetMessageAsBytes(OscString addressPattern, object[] arguments = null)
        {
            if (OscString.IsNullOrEmpty(addressPattern))
            {
                throw new ArgumentNullException(nameof(addressPattern), "OSC Serializer ERROR: Cannot convert message to bytes, its address pattern is null or empty.");
            }

            // check if the very first symbol of the address pattern is compliant to the standard
            if (addressPattern[0] != Separator)
            {
                throw new ArgumentException("OSC Serializer ERROR: Cannot convert, provided address pattern doesn't begin with a '/'.");
            }

            return GetMessageAsBytes(new OscMessage(addressPattern, arguments));

        }


        /// <summary>
        /// Serializes the provided OSC Bundle into a byte array.
        /// </summary>
        /// <param name="bundle"> OSC Bundle to be serialized. </param>
        /// <returns> An OSC Protocol-compliant byte array containing the serialized bundle. </returns>
        protected byte[] GetBundleAsBytes(OscBundle bundle)
        {
            byte[] binaryData = new byte[GetBundleOscLength(bundle)];

            int pointer = 0;

            AddBundleAsBytes(bundle, binaryData, ref pointer);

            return binaryData;

        }

        #endregion // GETTING BYTES OF WHOLE MESSAGES / BUNDLES



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
                extPointer += Chunk32;

                AddMessageAsBytes(messages[i], array, ref extPointer);

                int endPointer = extPointer;

                int length = endPointer - startPointer + Chunk32;

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
                extPointer += Chunk32;

                AddBundleAsBytes(bundles[i], array, ref extPointer);

                int endPointer = extPointer;

                int length = endPointer - startPointer + Chunk32;

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
                OscSerializer.AddBytes(packets[i].OscLength, array, ref extPointer);

                packets[i].BinaryData.CopyTo(array, extPointer);
                extPointer += packets[i].OscLength;
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
        protected byte[] GetBytesAsContent(OscMessage[] messages, out int length)
        {
            length = 0;

            int pointer = 0;

            for (int i = 0; i < messages.Length; i++)
            {
                // get length of all messages, plus allow 4 bytes for the "length" integer
                length += GetMessageOscLength(messages[i]) + OscProtocol.Chunk32;
            }

            byte[] binaryData = new byte[length];

            AddBytesAsContent(messages, binaryData, ref pointer);

            return binaryData;

        }


        /// <summary>
        /// Converts the provided OSC Bundles to bytes and orders them into a byte array of OSC Bundle "contents".
        /// </summary>
        /// <param name="bundles"> An array of OSC Messages to convert. </param>
        /// <param name="length"> An out parameter containing the length of the resultant byte array. </param>
        /// <returns> An ordered byte array of elements formatted as [element 1's length] - [element 1] - [element 2's length] - [element 2] and so on. </returns>
        protected byte[] GetBytesAsContent(OscBundle[] bundles, out int length)
        {
            length = 0;

            // find length
            for (int i = 0; i < bundles.Length; i++)
            {
                length += GetBundleOscLength(bundles[i]) + Chunk32;
            }

            byte[] binaryData = new byte[length];
            int pointer = 0;

            AddBytesAsContent(bundles, binaryData, ref pointer);
          
            return binaryData;

        }


        /// <summary>
        /// Orders the provided OSC Packets into a byte array of OSC Bundle "contents".
        /// </summary>
        /// <param name="packets"> An array of OSC Packets to order. </param>
        /// <param name="length"> An out parameter containing the length of the resultant byte array. </param>
        /// <returns> An ordered byte array of elements formatted as [element 1's length] - [element 1] - [element 2's length] - [element 2] and so on. </returns>
        protected byte[] GetBytesAsContent(OscPacket[] packets, out int length)
        {
            length = 0;

            // get length
            for (int i = 0; i < packets.Length; i++)
            {
                // length of each packet plus 4 bytes for recording the length itself
                length += packets[i].OscLength + Chunk32;
            }

            byte[] binaryData = new byte[length];
            int pointer = 0;

            AddBytesAsContent(packets, binaryData, ref pointer);

            return binaryData;

        }

        #endregion // GETTING BYTE "CONTENTS"


        #region PACKET SERIALIZATION

        /// <summary>
        /// Converts the provided 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public OscPacket GetPacket(OscMessage message)
        {
            return new OscPacket(GetMessageAsBytes(message));
        }


        public OscPacket GetPacket(OscString addressPattern, object[] arguments = null)
        {
            return new OscPacket(GetMessageAsBytes(addressPattern, arguments));
        }


        public OscPacket GetPacket(OscBundle bundle)
        {
            return new OscPacket(GetBundleAsBytes(bundle));
        }


        public OscPacket GetPacket(OscPacket[] packets)
        {
            int length = OscBundle.BundleHeaderLength;

            for (int i = 0; i < packets.Length; i++)
            {
                length += packets[i].OscLength + Chunk32;
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
                length += packets[i].OscLength + Chunk32;
            }

            byte[] data = new byte[length];

            AddBundleHeader(data, 0);

            int pointer = OscBundle.BundleHeaderLength;

            AddBytesAsContent(packets, data, ref pointer);

            return new OscPacket(data);
        }


        #endregion // PACKET SERIALIZATION



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
        public OscMessage GetMessage(byte[] data, ref int pointer, int length)
        {
            // should start with an '/'
            if (data[pointer] != Separator)
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
                    for (int i = pointer - 2; i >= pointer - Chunk32; i--)
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
                        addressPatternLength = pointer - msgStart - Chunk32;
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
            if (data[pointer] == Comma)
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
            if (oscPacket.BinaryData[0] != OscProtocol.Separator)
            {
                throw new ArgumentException("OSC Deserializer ERROR: Cannot deserialize OSC message, provided OSC Packet is invalid.");
            }

            return GetMessage(oscPacket.BinaryData);

        }


        public OscBundle GetBundle(byte[] data, ref int pointer, int length)
        {
            int bndStart = pointer;

            if (data[bndStart] != BundleMarker)
            {
                throw new ArgumentException("OSC Deserializer ERROR: Cannot deserialize OSC bundle, provided byte array is invalid.");
            }

            // let's skip the "#bundle" bit
            pointer += OscBundle.MarkerStringLength;

            // get timestamp
            OscTimetag timetag = OscDeserializer.GetTimetag(data, ref pointer);

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
                if (data[pointer] == BundleMarker)
                {
                    // element is a bundle, move ahead by the length of an element
                    bndTotal++;

                    pointer += elementLength;
                }
                else if (data[pointer] == Separator)
                {
                    // element is a message, move ahead by the length of an element
                    msgTotal++;

                    pointer += elementLength;
                }
                else
                {
                    // we got the length of next element here
                    elementLength = OscDeserializer.GetInt32(data, ref pointer);
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
                if (data[pointer] == BundleMarker)
                {
                    bundles[bndCount] = GetBundle(data, ref pointer, elementLength);
                    bndCount++;
                }
                else if (data[pointer] == Separator)
                {
                    messages[msgCount] = GetMessage(data, ref pointer, elementLength);
                    msgCount++;
                }
                else
                {
                    // there should be the length of next element here
                    elementLength = OscDeserializer.GetInt32(data, ref pointer);
                }

            }

            return new OscBundle(timetag, bundles, messages);

        }


        public OscBundle GetBundle(byte[] data)
        {
            int pointer = 0;

            return GetBundle(data, ref pointer, data.Length);
        }


        public OscBundle GetBundle<Packet>(Packet oscPacket) where Packet : IOscPacket
        {
            if (oscPacket.BinaryData[0] != OscProtocol.BundleMarker)
            {
                throw new ArgumentException("OSC Deserializer ERROR: Cannot deserialize OSC message, provided OSC Packet is invalid.");
            }

            return GetBundle(oscPacket.BinaryData);
        }

        #endregion


        #region GETTING ELEMENT LENGTH

        protected int GetMessageOscLength(OscMessage message)
        {
            int length = message.AddressPattern.OscLength;

            for (int i = 0; i < message.Arguments.Length; i++)
            {
                length += GetArgLength(message.Arguments[i]);
            }

            // account for the argument string length
            if (message.Arguments.Length > 0)
            {
                length += OscUtil.GetNextMultipleOfFour(message.Arguments.Length + 1);
            }
            else
            {
                if (_settingAddEmptyTypeTagStrings)
                {
                    // add space for a comma plus 3 empty bytes (how wasteful)
                    length += Chunk32;
                }

            }

            return length;
        }


        protected int GetBundleOscLength(OscBundle bundle)
        {
            int length = OscBundle.BundleHeaderLength;

            for (int i = 0; i < bundle.Messages.Length; i++)
            {
                length += GetMessageOscLength(bundle.Messages[i]);

                // account for length's bytes as well
                length += Chunk32;
            }

            for (int i = 0; i < bundle.Bundles.Length; i++)
            {
                length += GetBundleOscLength(bundle.Bundles[i]);

                length += Chunk32;
            }

            return length;
        }

        #endregion // GETTING ELEMENT LENGTH


        #region ABSTRACT METHODS

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
        protected abstract byte[] GetArgAsBytes<T>(T arg, out byte typeTag);


        /// <summary>
        /// Serializes the provided argument into its OSC byte data form and adds it into existing byte array, provides the corresponding type tag.  
        /// </summary>
        /// <remarks>
        /// Implemented the way it is to minimize boxing.
        /// </remarks>
        /// <param name="arg"> The argument to be serialized. </param>
        /// <param name="array"> The target byte array. </param>
        /// <param name="extPointer"> The index from which to add data. Will be shifted forwards by the length of added data. </param>
        /// <param name="typeTag"> Corresponding OSC type tag. </param>
        /// <exception cref="ArgumentException"> Thrown when the argument is of an unsupported type. </exception>
        protected abstract void AddArgAsBytes<T>(T arg, byte[] array, ref int extPointer, out byte typeTag);


        /// <summary>
        /// Returns the provided argument's OSC byte length.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="arg"></param>
        protected abstract int GetArgLength<T>(T arg);


        /// <summary>
        /// Deserializes the part of provided byte array into an object representing the OSC Message argument, starting from the pointer position and according to the provided type tag. 
        /// Shifts the pointer forward accordingly.
        /// </summary>
        /// <param name="array"> The byte array containing OSC byte data. </param>
        /// <param name="extPointer"> The external pointer designating the position from which the relevant data starts. </param>
        /// <param name="typeTag"> The type tag of the argument contained in the data. </param>
        /// <returns></returns>
        protected abstract object BytesToArg(byte[] array, ref int extPointer, byte typeTag);

        #endregion // ABSTRACT METHODS


        #region STATIC METHODS

        /// <summary>
        /// Checks whether the provided byte represents an ASCII symbol reserved by the OSC Protocol.
        /// </summary>
        /// <param name="symbol"> ASCII symbol as a byte. </param>
        /// <returns></returns>
        public static bool IsAReservedSymbol(byte symbol)
        {
            for (int i = 0; i < _addressReservedSymbols.Length; i++)
            {
                if (symbol == _addressReservedSymbols[i])
                    return true;
            }

            return false;

        }


        /// <summary>
        /// Checks whether the byte array contains any ASCII symbols reserved by the OSC Protocol.
        /// </summary>
        /// <param name="array"> An array presumably containing ASCII symbols as bytes. </param>
        /// <returns></returns>
        public static bool ContainsReservedSymbols(byte[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                if (IsAReservedSymbol(array[i]))
                {
                    return true;
                }

            }

            return false;
        }


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

            if (target.Length <= index + OscBundle.BundleHeaderLength)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "OSC Serializer ERROR: Can't add bundle header to byte array at index " + index + ", it won't fit. ");
            }

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

            if (target.Length <= index + OscBundle.BundleHeaderLength)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "OSC Serializer ERROR: Can't add bundle header to byte array at index " + index + ", it won't fit. ");
            }

            OscBundle.CopyMarkerStringTo(target, index);
            OscSerializer.AddBytes(timetag, target, index + Chunk64);

        }

        #endregion // ADDING BUNDLE HEADERS

        #endregion // STATIC METHODS

    }

}
