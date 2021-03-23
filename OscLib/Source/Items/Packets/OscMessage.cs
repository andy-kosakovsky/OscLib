using System;
using System.Text;

namespace OscLib
{
    /// <summary>
    /// Represents a single OSC message.
    /// </summary>
    public readonly struct OscMessage
    {
        // used to return an empty array of arguments
        private static readonly object[] _argumentsEmpty = new object[0];


        private readonly OscString _addressPattern;
        private readonly object[] _arguments;
        private readonly int _length;
       
        /// <summary> OSC address pattern of this message. </summary>
        public OscString AddressPattern { get => _addressPattern; }

        /// <summary> Arguments of this message. </summary>
        public object[] Arguments
        {
            get
            {
                if (_arguments == null)
                {
                    return _argumentsEmpty;
                }
                else
                {
                    return _arguments;
                }
            }

        }

        /// <summary> Length of this message in bytes. </summary>
        public int Length { get => _length; }


        /// <summary>
        /// Creates a new OSC message out of an address pattern and an array of arguments.
        /// </summary>
        /// <param name="addressPattern"> Address pattern attached to this message. </param>
        /// <param name="arguments"> An array of arguments attached to this message. </param>
        /// <exception cref="ArgumentException"> Thrown when address pattern is empty or invalid. </exception>
        /// <exception cref="ArgumentNullException"> Thrown when arguments array is null for some reason. </exception>
        public OscMessage(OscString addressPattern, object[] arguments)
        {
            if (addressPattern.Length < 1)
            {
                throw new ArgumentException("OscMessage ERROR: Cannot create an OSC message, address pattern is empty");
            }

            // check if address string is right
            if (addressPattern[0] != OscProtocol.Separator)
            {
                throw new ArgumentException("OscMessage ERROR: Cannot create an OSC Message, address pattern is invalid");
            }
         
            _addressPattern = addressPattern;
            _arguments = arguments;

            _length = _addressPattern.OscLength;


            if (_arguments != null)
            {
                for (int i = 0; i < arguments.Length; i++)
                {
                    _length += OscSerializer.GetByteLength(arguments[i]);
                }

                // account for the type tag string
                _length += OscUtil.GetNextMultipleOfFour(_arguments.Length + 1);

            }

        }


        /// <summary>
        /// Creates a new OSC message out of an address pattern, without arguments. Assumes that there is no type tag string at all.
        /// </summary>
        /// <param name="addressPattern"> Address pattern attached to this message. </param>   
        /// <exception cref="ArgumentException"> Thrown when address pattern is empty or invalid. </exception>
        public OscMessage(OscString addressPattern)
        {
            if (addressPattern.Length < 1)
            {
                throw new ArgumentException("OscMessage ERROR: Cannot create an OSC message, address pattern is empty");
            }

            // check if address string is right
            if (addressPattern[0] != OscProtocol.Separator)
            {
                throw new ArgumentException("OscMessage ERROR: Cannot create an OSC Message, address pattern is invalid");
            }

            _addressPattern = addressPattern;
            _arguments = null;

            _length = _addressPattern.OscLength;

        }


        /// <summary>
        /// Returns this message as a neatly formatted string, for debug purposes mostly.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder returnString = new StringBuilder();

            returnString.Append('(');
            returnString.Append(_length);
            returnString.Append(" bytes) ");
            returnString.Append("MESSAGE: ");
            returnString.Append(_addressPattern.ToString());

            if (_arguments != null)
            {
                returnString.Append("; Data: ");

                for (int i = 0; i < _arguments.Length; i++)
                {
                    if (_arguments[i] is byte[] dataBytes)
                    {
                        returnString.Append(BitConverter.ToString(dataBytes));
                    }
                    else
                    {
                        returnString.Append(_arguments[i]);
                    }

                    returnString.Append(", ");

                }

            }

            returnString.Append('\n');

            return returnString.ToString();

        }

    }
    
}
