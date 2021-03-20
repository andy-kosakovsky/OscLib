using System.Text;

namespace OscLib
{
    /// <summary>
    /// Represents a deserialized OSC message bundle.
    /// In a slight deviation from the spec, this struct CANNOT contain other bundles.
    /// </summary>
    /// <remarks>
    /// the idea behind this is that when deserializing incoming bundles, the bundles contained within other bundles are extracted (should their 
    /// timestamps not be less than that of the enveloping bundle). They are then dealt with according to their timestamps and the configuration
    /// of the OscReceiver. 
    /// The reason for it working the way it works is to 1) have both messages and bundles represented as structs, and 2) avoid using interfaces.
    /// All to do with boxing/unboxing and avoiding the dreaded gc pressure as much as possible.  
    /// </remarks>
    public readonly struct OscBundle
    {
        /// <summary> The length of the bundle header in bytes. That is, the "#bundle " + [timestamp] bit. </summary>
        public const int HeaderLength = 16;
        private readonly OscTimestamp _timestamp;

        // TODO: add bundles-in-bundles
        private readonly OscMessage[] _messages;
        // private readonly OscBundle[] _bundles;

        private readonly int _length;

        /// <summary> Timestamp attached to this bundle. </summary>
        public OscTimestamp Timestamp { get => _timestamp; }

        /// <summary> Messages inside this bundle. </summary>
        public OscMessage[] Messages { get => _messages; }

        /// <summary> Length of this bundle in bytes. </summary>
        public int Length { get => _length; }


        /// <summary>
        /// Creates an OSC bundle out of an array of OSC messages.
        /// </summary>
        /// <param name="timestamp"> A timestamp to stamp the time of this bundle's moment to shine. </param>
        /// <param name="messages"> An array of OSC messages to be contained within this bundle. </param>
        public OscBundle(OscTimestamp timestamp, OscMessage[] messages)
        {
            _timestamp = timestamp;
    
            _messages = messages;
            
            _length = HeaderLength;

            for (int i = 0; i < _messages.Length; i++)
            {
                // add message length plus the length of the integer containing its length
                _length += _messages[i].Length + OscProtocol.Chunk32;
            }
        }

        /// <summary>
        /// Creates an empty OSC Bundle. (Now why would you do that)
        /// </summary>
        /// <param name="timestamp"> A timestamp to stamp the time of this bundle's moment to shine. </param>
        public OscBundle(OscTimestamp timestamp)
        {
            _length = HeaderLength;
            _timestamp = timestamp;
            _messages = new OscMessage[0];
        }

        /// <summary>
        /// Returns this bundle as a neatly formatted string, for debug purposes mostly.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder returnString = new StringBuilder();

            returnString.Append("BUNDLE (DATA): ");
            returnString.Append(_timestamp.ToString());
            returnString.Append(", total length: ");
            returnString.Append(_length);
            returnString.Append('\n');


            if (_messages != null)
            {
                for (int i = 0; i < _messages.Length; i++)
                {
                    returnString.Append("   Message ");
                    returnString.Append(i);
                    returnString.Append(": ");
                    returnString.Append(_messages[i].ToString());
                }
            }

            return returnString.ToString();
        }

    }

}
