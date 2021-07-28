using System;
using System.Collections.Generic;
using System.Text;

namespace OscLib
{
    /// <summary>
    /// Implements a basic, bare-bones version of OSC Protocol, version 1.0 - only supporting Int32, Float32, Osc-String and Osc-Blob as arguments.
    /// <remarks>
    /// When sending out packets, arguments will be converted into one of the four supported data types:
    /// <para> Any integer value (both ints and uints) will be converted to int32, getting clamped accordingly; </para>
    /// <para> OSC Timetags will be clamped to an int32 value containing the integer part of the underlying NTP timestamp. </para>
    /// <para> Any floating-point value will be converted to float32; </para>
    /// <para> Any OSC Protocol-designated types (OscColor, etc) will be converted into binary OSC-blobs where possible; </para>
    /// <para> Anything else will be converted to ASCII strings. </para>
    /// </remarks>
    /// </summary>
    public class OscV1_0Mini : OscConverter
    {
        private const byte _int32 = (byte)'i';
        private const byte _float32 = (byte)'f';
        private const byte _string = (byte)'s';
        private const byte _blob = (byte)'b';

        // used to handle nulls
        private const string _nullString = "NULL";

        public OscV1_0Mini()
        {
            // set it to add empty address strings by default
            _addEmptyTypeTagStrings = true;
        }


        protected override void AddArgAsBytes<T>(T arg, byte[] array, ref int extPointer, out byte typeTag)
        {
            switch (arg)
            {
                // ints
                case int argInt:
                    typeTag = _int32;
                    OscSerializer.AddBytes(argInt, array, ref extPointer);
                    break;

                case short argShort:
                    typeTag = _int32;
                    OscSerializer.AddBytes((int)argShort, array, ref extPointer);
                    break;

                case long argLong:
                    typeTag = _int32;
                    OscSerializer.AddBytes(argLong.ClampToInt(), array, ref extPointer);
                    break;

                case uint argUint:
                    typeTag = _int32;
                    OscSerializer.AddBytes((int)argUint, array, ref extPointer);
                    break;

                case ushort argUshort:
                    typeTag = _int32;
                    OscSerializer.AddBytes((int)argUshort, array, ref extPointer);
                    break;

                case ulong argUlong:
                    typeTag = _int32;
                    OscSerializer.AddBytes(argUlong.ClampToInt(), array, ref extPointer);
                    break;

                case byte argByte:
                    typeTag = _int32;
                    OscSerializer.AddBytes((int)argByte, array, ref extPointer);
                    break;

                case OscTimetag argTimetag:
                    typeTag = _int32;
                    OscSerializer.AddBytes((uint)(argTimetag.NtpTimestamp >> 32).ClampToInt(), array, ref extPointer);
                    break;


                // floats
                case float argFloat:
                    typeTag = _float32;
                    OscSerializer.AddBytes(argFloat, array, ref extPointer);
                    break;

                case double argDouble:
                    typeTag = _float32;
                    OscSerializer.AddBytes((float)argDouble, array, ref extPointer);
                    break;

                case decimal argDecimal:
                    typeTag = _float32;
                    OscSerializer.AddBytes((float)argDecimal, array, ref extPointer);
                    break;


                // strings
                case string argString:
                    typeTag = _string;
                    OscSerializer.AddBytes(argString, array, ref extPointer);
                    break;

                case OscString oscString:
                    typeTag = _string;
                    OscSerializer.AddBytes(oscString, array, ref extPointer);
                    break;

                case byte[] argBlob:
                    typeTag = _blob;
                    OscSerializer.AddBytes(argBlob, array, ref extPointer);
                    break;

                // check if it's convertible to blob
                case IOscBlobbable futureBlob:
                    typeTag = _blob;
                    futureBlob.AddAsBlob(array, ref extPointer);
                    break;

                // if argument type is not supported, convert it to string and add as such
                default:
                    typeTag = _string;

                    if (arg == null)
                    {
                        OscSerializer.AddBytes(_nullString, array, ref extPointer);
                    }
                    else
                    {
                        OscSerializer.AddBytes(arg.ToString(), array, ref extPointer);
                    }
                    break;

            }

        }


        protected override object BytesToArg(byte[] array, ref int extPointer, byte typeTag)
        {
            switch (typeTag)
            {
                case _int32:
                    return OscDeserializer.GetInt32(array, ref extPointer);

                case _float32:
                    return OscDeserializer.GetFloat32(array, ref extPointer);

                case _string:
                    return OscDeserializer.GetString(array, ref extPointer);

                case _blob:
                    return OscDeserializer.GetBlob(array, ref extPointer);

                default:
                    throw new ArgumentException("OSC Converter ERROR: Can't deserialize argument, argument type is not supported.");
            }

        }

        
        protected override int GetArgOscSize<T>(T arg)
        {
            switch (arg)
            {
                // integer values will always be clamped to 32bit in this particular implementation
                case int _:
                case short _:
                case long _:
                case uint _:
                case ushort _:
                case ulong _:
                case byte _:                
                    return OscProtocol.Chunk32;

                // ditto for any kind of fixed- or floating-point value
                case float _:
                case double _:
                case decimal _:
                case OscTimetag _:
                    return OscProtocol.Chunk32;

                case string argString:
                    return OscSerializer.GetOscLength(argString);

                case OscString oscString:
                    return OscSerializer.GetOscLength(oscString);

                case byte[] argBlob:
                    return OscSerializer.GetOscLength(argBlob);

                case IOscBlobbable blobbable:
                    return blobbable.SizeAsBlob;

                default:
                    if (arg == null)
                    {
                        return OscSerializer.GetOscLength(_nullString);
                    }

                    return OscSerializer.GetOscLength(arg.ToString());

            }

        }

    }
}
