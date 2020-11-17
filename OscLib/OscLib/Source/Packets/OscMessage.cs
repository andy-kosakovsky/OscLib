using System;
using System.Text;

namespace OscLib
{
    /// <summary>
    /// Represents a single OSC message.
    /// </summary>
    public readonly struct OscMessage
    {
        private readonly OscString _addressPattern;
        private readonly object[] _arguments;
        private readonly int _length;
       
        /// <summary> OSC address pattern of this message. </summary>
        public OscString AddressPattern { get => _addressPattern; }

        /// <summary> Arguments of this message. </summary>
        public object[] Arguments { get => _arguments; }

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
            if (addressPattern[0] != OscProtocol.SymbolAddressSeparator)
            {
                throw new ArgumentException("OscMessage ERROR: Cannot create an OSC Message, address pattern is invalid");
            }

            if (arguments == null)
            {
                throw new ArgumentNullException(nameof(arguments));
            }

            _addressPattern = addressPattern;
            _arguments = arguments;

            _length = _addressPattern.OscLength;

            for (int i = 0; i < arguments.Length; i++)
            {
                _length += OscSerializer.GetArgumentLength(arguments[i]);
            }

            // account for the type tag string
            _length += OscUtil.GetNextMultipleOfFour(_arguments.Length + 1);

        }


        /// <summary>
        /// Creates a new OSC message out of an address pattern, without arguments.
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
            if (addressPattern[0] != OscProtocol.SymbolAddressSeparator)
            {
                throw new ArgumentException("OscMessage ERROR: Cannot create an OSC Message, address pattern is invalid");
            }

            _addressPattern = addressPattern;
            _arguments = new object[0];

            _length = _addressPattern.OscLength;

            // account for the type tag string
            _length += OscUtil.GetNextMultipleOfFour(_arguments.Length + 1);

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
            returnString.Append("MESSAGE (DATA): ");
            returnString.Append(_addressPattern.ToString());
            returnString.Append("; Data: ");


            for (int i = 0; i < _arguments.Length; i++)
            {
                if (_arguments[i] is byte[] dataBytes)
                    returnString.Append(BitConverter.ToString(dataBytes));
                else
                    returnString.Append(_arguments[i]);
                returnString.Append(", ");
            }

            returnString.Append('\n');

            return returnString.ToString();

        }

    }
    
}
