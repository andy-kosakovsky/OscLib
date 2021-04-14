using System;
using System.Collections.Generic;
using System.Text;

namespace OscLib
{
    /// <summary>
    /// An implementation of a "full" version of OSC Protocol - supporting all additional data types designated in the spec.
    /// </summary>
    public class OscConvertFull : OscConvert
    {
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

        // arrays
        private const byte _arrayOpen = (byte)'[';
        private const byte _arrayClose = (byte)']';
        
        public OscConvertFull()
        {
            // set the settings
            _settingEmptyTypeTagStrings = true;
        }



        protected override byte[] GetArgAsBytes<T>(T arg, out byte typeTag)
        {
            int length = GetArgLength(arg);
            int pointer = 0;

            byte[] data = new byte[length];

            AddArgAsBytes(arg, data, ref pointer, out typeTag);

            return data;
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

                default:
                    throw new Exception();
                

            }

        }


        protected override T BytesToArg<T>(byte[] array, ref int extPointer, byte typeTag) 
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
                    throw new ArgumentException("OSC Protocol ERROR: Can't deserialize argument, argument type is not supported.");
            }

        }

        
        protected override int GetArgLength<T>(T arg)
        {
            switch (arg)
            {
                // 64bit vars will be shortened to 32bit in this particular implementation
                case int _:
                case float _:
                case long _:
                case double _:
                case OscTimetag _:
                case ulong _:
                    return Chunk32;

                case string argString:
                    return OscSerializer.GetLength(argString);

                case OscString oscString:
                    return OscSerializer.GetLength(oscString);

                case byte[] argBlob:
                    return OscSerializer.GetLength(argBlob);

                default:
                    return OscSerializer.GetLength(arg.ToString());

            }

        }

    }
}
