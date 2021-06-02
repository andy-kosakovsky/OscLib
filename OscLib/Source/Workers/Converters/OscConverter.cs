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
    /// floats, some use an integer equal to 1 or 0 instead of T and F typetags for sending booleans, some use custom typetags, and so on. Some applications don't even allow any arguments
    /// apart from the bare standard int32, float32, OSC-string and OSC-blob. All this can be accounted for in method overloads in the derived classes. 
    /// </remarks>
    public abstract class OscConverter
    {

        /// <summary> Controls whether this version of OSC Protocol demands adding an empty type tag string (that is, a comma followed by three null bytes) when there aren't any arguments. </summary>
        protected bool _settingEmptyTypeTagStrings;

        /// <summary> Controls whether this version of OSC Protocol demands adding an empty type tag string (that is, a comma followed by three null bytes) when there aren't any arguments. </summary>
        public virtual bool SettingEmptyTypeTagStrings { get => _settingEmptyTypeTagStrings; set => _settingEmptyTypeTagStrings = value; }


        #region EVENTS




        #endregion // EVENTS



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

       
            message.AddressPattern.CopyBytesToArray(array, msgStart);

            extPointer += addrLength;

            if (message.ArgumentsCount > 0)
            {
                // add the address pattern comma
                array[msgStart + addrLength] = OscProtocol.Comma;

                // find the length of type tag, " + 1" accounts for the comma
                int typeTagLength = OscUtil.GetNextMultipleOfFour(message.ArgumentsCount + 1);

                extPointer = msgStart + addrLength + typeTagLength;

                for (int i = 0; i < message.ArgumentsCount; i++)
                {
                    AddArgAsBytes(message[i], array, ref extPointer, out array[msgStart + addrLength + 1 + i]);
                }

            }
            else
            {
                if (_settingEmptyTypeTagStrings)
                {
                    // add the address pattern comma
                    array[msgStart + addrLength] = OscProtocol.Comma;

                    // shift pointer forward by 4 bytes - the comma plus three null bytes
                    extPointer += OscProtocol.Chunk32;
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
                OscSerializer.AddBytes(packets[i].Length, array, ref extPointer);

                packets[i].CopyBytesToArray(array, extPointer);
                extPointer += packets[i].Length;
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
            byte[] binaryData = new byte[GetMessageOscLength(message)];

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

            byte[] binaryData = new byte[GetMessageOscLength(message)];

            int fakePointer = 0;

            AddMessageAsBytes(message, binaryData, ref fakePointer);

            return new OscPacket(binaryData);
        }


        public OscPacket GetPacket(OscBundle bundle)
        {
            byte[] binaryData = new byte[GetBundleOscLength(bundle)];

            int fakePointer = 0;

            AddBundleAsBytes(bundle, binaryData, ref fakePointer);

            return new OscPacket(binaryData);
        }


        public OscPacket GetPacket(OscPacket[] packets)
        {
            int length = OscBundle.BundleHeaderLength;

            for (int i = 0; i < packets.Length; i++)
            {
                length += packets[i].Length + OscProtocol.Chunk32;
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
                length += packets[i].Length + OscProtocol.Chunk32;
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
            // should start with an '/'
            if (data[extPointer] != OscProtocol.Separator)
            {
                throw new ArgumentException("OSC Deserializer ERROR: No OSC Message found at pointer position, expecting a '/' symbol. Pointer at: " + extPointer);
            }

            OscString addressPattern;

            object[] arguments = null;

            int addressPatternLength = -1, typeTagStart = -1;

            int msgStart = extPointer;

            // find the address pattern
            while (extPointer < msgStart + length)
            {
                // move pointer forward by a chunk
                extPointer += OscProtocol.Chunk32;

                // preceding chunk ending in a 0 means the pattern ends somewhere within it, or right at the end of the chunk before it.
                if (data[extPointer - 1] == 0)
                {
                    for (int i = extPointer - 2; i >= extPointer - OscProtocol.Chunk32; i--)
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
                        addressPatternLength = extPointer - msgStart - OscProtocol.Chunk32;
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


            if (extPointer >= msgStart + length)
            {
                // if we've reached the end of a a message already, and we don't expect an empty type tag string, we might as well return it 
                if (!_settingEmptyTypeTagStrings)
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

                // find the end of it
                while (data[extPointer] != 0)
                {
                    extPointer++;
                }

                // - 1 is to accomodate for the "," in the beginning
                int typeTagsTotal = extPointer - typeTagStart - 1;

                arguments = new object[typeTagsTotal];

                // move the pointer to the next 4-byte point after the end of the type tags
                extPointer = OscUtil.GetNextMultipleOfFour(extPointer);

                if (typeTagsTotal > 0)
                {
                    // the first element in the array will be the "," type tag separator
                    for (int i = 0; i < typeTagsTotal; i++)
                    {
                        arguments[i] = BytesToArg(data, ref extPointer, data[typeTagStart + 1 + i]);
                        
                        if (extPointer > msgStart + length)
                        {
                            throw new InvalidOperationException("OSC Deserializer ERROR: Pointer went beyond the end of message. ");
                        }

                    }

                }

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
            return GetMessage(oscPacket.GetBytes());
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
                throw new ArgumentException("OSC Deserializer ERROR: Cannot deserialize OSC bundle, provided byte array is invalid.");
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
        /// <typeparam name="Packet"> The packet struct fed into this method should implement the IOscPacket interface. Obviously, it should contain readable OSC data as well. </typeparam>
        /// <param name="oscPacket"></param>
        /// <returns></returns>
        public OscBundle GetBundle<Packet>(Packet oscPacket) where Packet : IOscPacket
        {
            return GetBundle(oscPacket.GetBytes());
        }

        #endregion



        #region GETTING ELEMENT LENGTH

        /// <summary>
        /// Calculates the byte length of the provided OSC Message according to this OSC Protocol implementation's specifics.
        /// </summary>
        /// <param name="message"> The message to measure. </param>
        /// <returns> The length of the provided message in bytes. </returns>
        public int GetMessageOscLength(OscMessage message)
        {
            int length = message.AddressPattern.OscLength;

            for (int i = 0; i < message.ArgumentsCount; i++)
            {
                length += GetArgLength(message[i]);
            }

            // account for the argument string length
            if (message.ArgumentsCount > 0)
            {
                length += OscUtil.GetNextMultipleOfFour(message.ArgumentsCount + 1);
            }
            else
            {
                if (_settingEmptyTypeTagStrings)
                {
                    // add space for a comma plus 3 empty bytes (how wasteful)
                    length += OscProtocol.Chunk32;
                }

            }

            return length;
        }

        /// <summary>
        /// Calculates the byte length of the provided OSC Bundle according to this OSC Protocol implementation's specifics.
        /// </summary>
        /// <param name="bundle"> The bundle to measure. </param>
        /// <returns> The length of the provided message in bytes. </returns>
        public int GetBundleOscLength(OscBundle bundle)
        {
            int length = OscBundle.BundleHeaderLength;

            for (int i = 0; i < bundle.Messages.Length; i++)
            {
                length += GetMessageOscLength(bundle.Messages[i]);

                // account for length's bytes as well
                length += OscProtocol.Chunk32;
            }

            for (int i = 0; i < bundle.Bundles.Length; i++)
            {
                length += GetBundleOscLength(bundle.Bundles[i]);

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
            int length = GetArgLength(arg);
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
        /// Returns the provided argument's OSC byte length.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="arg"></param>
        protected abstract int GetArgLength<T>(T arg);


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
        /// <returns></returns>
        protected abstract object BytesToArg(byte[] array, ref int extPointer, byte typeTag);


        #endregion // ABSTRACT METHODS



        #region STATIC METHODS

      

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

        #endregion // ADDING BUNDLE HEADERS

        #endregion // STATIC METHODS

    }

}
