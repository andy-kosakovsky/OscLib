using System;
using System.Collections.Generic;
using System.Text;

namespace OscLib
{
    /// <summary>
    /// An "full" implementation of OSC Protocol version 1.0 - supporting all additional data types designated in the spec.
    /// </summary>
    public class OscV1_0Full : OscConverter
    {
        //TODO: ADD ARRAYS TO OSC V1.0

        // standard types
        private const byte _int32 = (byte)'i';
        private const byte _float32 = (byte)'f';
        private const byte _string = (byte)'s';
        private const byte _blob = (byte)'b';

        //64 bit stuff
        private const byte _int64 = (byte)'h';
        private const byte _timetag = (byte)'t';
        private const byte _float64 = (byte)'d';
        
        // extras
        private const byte _stringAlt = (byte)'S';
        private const byte _char = (byte)'c';
        private const byte _color = (byte)'r';
        private const byte _midi = (byte)'m';

        // argument-less type tags (encoding an argument in itself)
        private const byte _true = (byte)'T';
        private const byte _false = (byte)'F';
        private const byte _nil = (byte)'N';
        private const byte _inf = (byte)'I';
      
        public OscV1_0Full()
        {
            // set the settings
            _addEmptyTypeTagStrings = true;
        }


        protected override void AddArgAsBytes<T>(T arg, byte[] array, ref int extPointer, out byte typeTag)
        {
            switch (arg)
            {
                // cover the basics
                case int argInt:
                    typeTag = _int32;
                    OscSerializer.AddBytes(argInt, array, ref extPointer);
                    break;

                case float argFloat:
                    if (float.IsInfinity(argFloat))
                    {
                        typeTag = _inf;
                        break;
                    }
                    
                    if (float.IsNaN(argFloat))
                    {
                        typeTag = _nil;
                        break;
                    }
                    
                    typeTag = _float32;
                    OscSerializer.AddBytes(argFloat, array, ref extPointer);
                    
                    break;

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

                // 64-bit stuff
                case long argLong:
                    typeTag = _int64;
                    OscSerializer.AddBytes(argLong, array, ref extPointer);
                    break;

                case double argDouble:
                    if (double.IsInfinity(argDouble))
                    {
                        typeTag = _inf;
                        break;
                    }

                    if (double.IsNaN(argDouble))
                    {
                        typeTag = _nil;
                        break;
                    }

                    typeTag = _float64;
                    OscSerializer.AddBytes(argDouble, array, ref extPointer);
                    break;

                case OscTimetag argTimetag:
                    typeTag = _timetag;
                    OscSerializer.AddBytes(argTimetag, array, ref extPointer);
                    break;

                // extras
                case OscColor argColor:
                    typeTag = _color;
                    OscSerializer.AddBytes(argColor, array, ref extPointer);
                    break;

                case OscMidi argMidi:
                    typeTag = _midi;
                    OscSerializer.AddBytes(argMidi, array, ref extPointer);
                    break;

                // TODO: make sure that char-over-osc is correct
                case char argChar:
                    byte trueChar = (byte)argChar;
                    
                    typeTag = _char;
                    OscSerializer.AddBytes((int)trueChar, array, ref extPointer);
                    break;

                case bool boolean:
                    if (boolean)
                        typeTag = _true;
                    else
                        typeTag = _false;
                    // don't add any bytes to the byte array
                    break;

                default:
                    if (arg == null)
                    {
                        typeTag = _nil;
                        break;
                    }

                    throw new ArgumentException("OSC Converter ERROR: Cannot convert argument to bytes, argument of unsupported type.");
               
            }

        }


        protected override object BytesToArg(byte[] array, ref int extPointer, byte typeTag)
        {
            switch (typeTag)
            {
                // basics
                case _int32:
                    return OscDeserializer.GetInt32(array, ref extPointer);

                case _float32:
                    return OscDeserializer.GetFloat32(array, ref extPointer);

                case _string:
                    return OscDeserializer.GetString(array, ref extPointer);

                case _blob:
                    return OscDeserializer.GetBlob(array, ref extPointer);

                // 64bit stuff
                case _int64:
                    return OscDeserializer.GetInt64(array, ref extPointer);

                case _float64:
                    return OscDeserializer.GetFloat64(array, ref extPointer);

                case _timetag:
                    return OscDeserializer.GetTimetag(array, ref extPointer);

                // special bits
                case _stringAlt:
                    return OscDeserializer.GetOscString(array, ref extPointer);

                case _char:
                    int intChar = OscDeserializer.GetInt32(array, ref extPointer);
                    return (char)intChar;

                case _color:
                    return OscDeserializer.GetOscColor(array, ref extPointer);

                case _midi:
                    return OscDeserializer.GetOscMidi(array, ref extPointer);

                // type-tag-only bits
                case _nil:
                    return null;

                case _inf:
                    return float.PositiveInfinity;

                case _true:
                    return true;

                case _false:
                    return false;

                default:
                    throw new ArgumentException("OSC Converter ERROR: Can't deserialize argument, argument type is not supported.");
            }

        }

        
        protected override int GetArgOscSize<T>(T arg)
        {
            switch (arg)
            {
                case int _:
                case char _:
                case OscMidi _:
                case OscColor _:
                    return OscProtocol.Chunk32;

                case long _:               
                case OscTimetag _:
                    return OscProtocol.Chunk64;

                // zero-length args
                case float argFloat:
                    if (float.IsInfinity(argFloat))
                        return 0;
                    else
                        return OscProtocol.Chunk32;

                case double argDouble:
                    if (double.IsInfinity(argDouble))
                        return 0;
                    else
                        return OscProtocol.Chunk64;

                case bool _:
                    return 0;
               
                // the rest
                case string argString:
                    return OscSerializer.GetOscLength(argString);

                case OscString oscString:
                    return OscSerializer.GetOscLength(oscString);

                case byte[] argBlob:
                    return OscSerializer.GetOscLength(argBlob);

                default:
                    if (arg == null)
                        return 0;
                    else
                        throw new ArgumentException("OSC Converter ERROR: Can't deserialize argument, argument type is not supported.");

            }

        }

    }

}
