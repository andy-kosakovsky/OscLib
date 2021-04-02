using System.Text;

namespace OscLib
{
    /// <summary>
    /// Represents a deserialized OSC packet bundle.
    /// May contain both messages and bundles. Bundles inside a bundle should be time-tagged 
    /// </summary>

    public readonly struct OscBundle
    {
        /// <summary> The length of the bundle header in bytes. That is, the "#bundle " + [timestamp] bit. </summary>
        public const int HeaderLength = 16;

        // these are used to return empty arrays of messages and bundles
        private static readonly OscMessage[] _messagesEmpty = new OscMessage[0];
        private static readonly OscBundle[] _bundlesEmpty = new OscBundle[0];

        private readonly OscTimetag _timetag;

        private readonly OscMessage[] _messages;
        private readonly OscBundle[] _bundles;

        private readonly int _oscLength;

        /// <summary> Timestamp attached to this bundle. </summary>
        public OscTimetag Timetag { get => _timetag; }

        /// <summary> Messages inside this bundle. </summary>
        public OscMessage[] Messages 
        {
            get
            { 
                if (_messages != null)
                {
                    return _messages;
                }
                else
                {
                    return _messagesEmpty;
                }
            }

        }

        /// <summary> Bundles inside this bundle. </summary>
        public OscBundle[] Bundles 
        {
            get
            {
                if (_bundles != null)
                {
                    return _bundles;
                }
                else
                {
                    return _bundlesEmpty;
                }

            }

        }

        /// <summary> Length of this bundle in bytes. </summary>
        public int OscLength { get => _oscLength; }


        /// <summary>
        /// Creates an OSC Bundle out of an array of messages and an array of bundles.
        /// </summary>
        /// <param name="timetag"> An OSC Timetag of this bundle. </param>
        /// <param name="bundles"> An array of OSC Bundles to be packed into this bundle. </param>
        /// <param name="messages"> An array of OSC Messages to be packed into this bundle. </param>
        public OscBundle(OscTimetag timetag, OscBundle[] bundles, OscMessage[] messages)
        {
            _timetag = timetag;

            _messages = messages;
            _bundles = bundles;

            // find the length for everything
            _oscLength = HeaderLength;

            if (_bundles != null)
            {
                for (int i = 0; i < _bundles.Length; i++)
                {
                    // add bundle length plus the length of the integer containing its length
                    _oscLength += _bundles[i].OscLength + OscProtocol.Chunk32;
                }

            }

            if (_messages != null)
            {
                for (int i = 0; i < _messages.Length; i++)
                {
                    // ditto
                    _oscLength += _messages[i].OscLength + OscProtocol.Chunk32;
                }

            }

        }


        /// <summary>
        /// Creates an OSC bundle out of an array of OSC messages.
        /// </summary>
        /// <param name="timetag"> An OSC Timetag of this bundle. </param>
        /// <param name="bundles"> An array of OSC messages to be packed into this bundle. </param>
        public OscBundle(OscTimetag timetag, OscBundle[] bundles)
        {
            _timetag = timetag;

            _messages = null;
            _bundles = bundles;

            // find the length of everything
            _oscLength = HeaderLength;

            if (_messages != null)
            {
                for (int i = 0; i < _bundles.Length; i++)
                {
                    // add message length plus the length of the integer containing its length
                    _oscLength += _bundles[i].OscLength + OscProtocol.Chunk32;
                }

            }

        }


        /// <summary>
        /// Creates an OSC bundle out of an array of OSC messages.
        /// </summary>
        /// <param name="timetag"> An OSC Timetag of this bundle. </param>
        /// <param name="messages"> An array of OSC messages to be packed into this bundle. </param>
        public OscBundle(OscTimetag timetag, OscMessage[] messages)
        {
            _timetag = timetag;
    
            _messages = messages;
            _bundles = null;
            
            // find the length of everything
            _oscLength = HeaderLength;

            if (_messages != null)
            {
                for (int i = 0; i < _messages.Length; i++)
                {
                    // add message length plus the length of the integer containing its length
                    _oscLength += _messages[i].OscLength + OscProtocol.Chunk32;
                }

            }

        }


        /// <summary>
        /// Creates an empty OSC Bundle. Now why would you do that.
        /// </summary>
        /// <param name="timetag"> A OSC Timetag of this sad empty bundle. </param>
        public OscBundle(OscTimetag timetag)
        {
            _oscLength = HeaderLength;

            _timetag = timetag;

            // nothing to see here
            _messages = null;
            _bundles = null;

        }

        /// <summary>
        /// Returns this bundle as a neatly formatted string, for debug purposes mostly.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return ToShiftedString(0);
        }


        /// <summary>
        /// A debug method to print bundles inside of bundles easier. Does the same thing as the standard ToSpring() but adds some spaces after every new line.
        /// </summary>
        /// <param name="shiftAmount">How many spaces to add aftear each new line</param>
        /// <returns></returns>
        public string ToShiftedString(int shiftAmount = 6)
        {
            string spaces = OscUtil.GetRepeatingChar(' ', shiftAmount);

            StringBuilder returnString = new StringBuilder();

            returnString.Append(spaces);
            returnString.Append("BUNDLE:-----------------------------------------\n");
            returnString.Append(spaces);
            returnString.Append("Time tag: ");
            returnString.Append(_timetag.ToString());
            returnString.Append(", total length: ");
            returnString.Append(_oscLength);

            if (_bundles != null)
            {
                returnString.Append('\n');
                returnString.Append(spaces);
                returnString.Append("Bundles inside: ");
                returnString.Append(_bundles.Length);
            }

            if (_messages != null)
            {
                returnString.Append("; messages inside: ");
                returnString.Append(_messages.Length);
            }           

            if ((_bundles != null) && (_bundles.Length > 0))
            {
                returnString.Append('\n');
                for (int i = 0; i < _bundles.Length; i++)
                {               
                    returnString.Append('\n');
                    returnString.Append(spaces);
                    returnString.Append(_bundles[i].ToShiftedString(shiftAmount + 6));
                }
             
            }

            if ((_messages != null) && (_messages.Length > 0))
            {
                returnString.Append('\n');

                for (int i = 0; i < _messages.Length; i++)
                {
                    returnString.Append('\n');
                    returnString.Append(spaces);
                    returnString.Append("Message ");
                    returnString.Append(i);
                    returnString.Append(": ");
                    returnString.Append(_messages[i].ToString());
                }

                returnString.Append('\n');
                returnString.Append(spaces);
                returnString.Append("END BUNDLE:-------------------------------------\n");
                returnString.Append('\n');
            }

            return returnString.ToString();
        }

    }

}
