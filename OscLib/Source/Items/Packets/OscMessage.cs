using System;
using System.Text;

namespace OscLib
{
    /// <summary>
    /// Represents a single deserialized OSC Message.
    /// </summary>
    public readonly struct OscMessage 
    {
        // used to return an empty array of arguments
        private static readonly object[] _argumentsEmpty = new object[0];

        private readonly OscString _addressPattern;
        private readonly object[] _arguments;

        /// <summary> The address pattern of this Message. </summary>
        public OscString AddressPattern { get => _addressPattern; }

        /// <summary> The number of arguments inside this Message. </summary>
        public int ArgumentsCount
        {
            get
            {
                if (_arguments != null)
                {
                    return _arguments.Length;
                }
                else
                {
                    return 0;
                }

            }

        }


        /// <summary>
        /// Indexer access to arguments contained in this Message.
        /// </summary>
        /// <param name="index"> Argument index. </param>
        /// <returns> The argument at the specified index, or null if index is out of bounds. </returns>
        public object this[int index]
        {
            get
            {
                if (index.IsNumberBetween(0, _arguments.Length - 1))
                {
                    return _arguments[index];
                }
                else
                {
                    return null;
                }

            }

        }
        

        /// <summary>
        /// Creates a new OSC Message out of an address pattern and an array of arguments.
        /// </summary>
        /// <param name="addressPattern"> Address pattern attached to this message. </param>
        /// <param name="arguments"> An array of arguments attached to this message. Can be null if no arguments are needed. </param>
        /// <exception cref="ArgumentException"> Thrown when the address pattern is empty or invalid. </exception>
        public OscMessage(OscString addressPattern, object[] arguments = null)
        {
            if (OscString.IsNullOrEmpty(addressPattern))
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

        }


        /// <summary>
        /// Retrieves the array of arguments attached to this Message.
        /// </summary>
        /// <remarks> Despite being read-only, one can still change individual values inside this array. This can be both good and bad - and definitely something to be wary of.
        /// If this behaviour is not explicitly required, and neither is having access to the entire argument array, it's probably safer to use indexer access instead. </remarks>
        /// <returns> An array of arguments, or an empty array if there are none. </returns>
        public object[] GetArguments()
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


        /// <summary>
        /// Prints out the contents of this Message as a neatly-formatted string.
        /// </summary>
        public override string ToString()
        {
            StringBuilder returnString = new StringBuilder();

            returnString.Append("MESSAGE: ");
            returnString.Append(_addressPattern.ToString());

            if (_arguments != null)
            {
                returnString.Append("; Arguments: ");

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
