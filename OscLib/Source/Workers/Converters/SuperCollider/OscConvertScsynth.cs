using System;
using System.Collections.Generic;
using System.Text;

namespace OscLib
{
    /// <summary>
    /// Used to communicate with SuperCollider's scsynth part.
    /// </summary>
    public class OscConvertScsynth : OscConverter
    {

        private const byte _int32 = (byte)'i';
        private const byte _float32 = (byte)'f';
        private const byte _string = (byte)'s';
        private const byte _blob = (byte)'b';

        private const byte _float64 = (byte)'d';

        // used to handle nulls
        private const string _nullString = "NULL";

        public OscConvertScsynth()
        {
            // set it to add empty address strings by default
            _settingEmptyTypeTagStrings = true;
        }


        protected override void AddArgAsBytes<T>(T arg, byte[] array, ref int extPointer, out byte typeTag)
        {
            switch (arg)
            {
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

                // modify some of the args to fit the limits of this version of the protocol
                case long argLong:
                    typeTag = _int32;
                    OscSerializer.AddBytes(OscUtil.ClampLong(argLong), array, ref extPointer);
                    break;

                case double argDouble:
                    typeTag = _float64;
                    OscSerializer.AddBytes(argDouble, array, ref extPointer);
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

                case _float64:
                    return OscDeserializer.GetFloat64(array, ref extPointer);

                case _string:
                    return OscDeserializer.GetString(array, ref extPointer);

                case _blob:
                    return OscDeserializer.GetBlob(array, ref extPointer);
           
                default:
                    throw new ArgumentException("OSC Converter ERROR: Can't deserialize argument, argument type is not supported.");
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
                case OscTimetag _:
                    return OscProtocol.Chunk32;

                case double _:
                    return OscProtocol.Chunk64;

                case string argString:
                    return OscSerializer.GetOscLength(argString);

                case OscString oscString:
                    return OscSerializer.GetOscLength(oscString);

                case byte[] argBlob:
                    return OscSerializer.GetOscLength(argBlob);

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

