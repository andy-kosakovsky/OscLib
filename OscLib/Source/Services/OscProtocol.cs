
namespace OscLib
{
    /// <summary>
    /// Contains common constants, values and symbols as defined by the Open Sound Control Protocol specification.
    /// </summary>
    public static class OscProtocol
    {
        // byte lengths of data chunks used by OSC

        /// <summary> Length in bytes of a single (32 bits/4 bytes long) OSC data chunk. </summary>
        public const int Chunk32 = 4;
        /// <summary> Length in bytes of a double-sized (64 bits/8 bytes long) OSC data chunk. </summary>
        public const int Chunk64 = 8;


        #region PATTERN MATCHING CONSTS

        /// <summary> Stands for any sequence of zero or more characters in pattern matching. </summary>
        public const byte MatchAnySequence = (byte)'*';

        /// <summary> Stands for any single character in pattern matching. </summary>
        public const byte MatchAnyChar = (byte)'?';


        /// <summary> Opens an array of characters in pattern matching. A match will occur if any of the characters within the array corresponds to a single character. </summary>
        public const byte MatchCharArrayOpen = (byte)'[';

        /// <summary> Closes an array of characters in pattern matching. </summary>
        public const byte MatchCharArrayClose = (byte)']';

        /// <summary> "Reverses" the character array, matching it with any symbol *not* present in it.  </summary>
        public const byte MatchNot = (byte)'!';

        /// <summary> A range symbol used inside character arrays. Stands for the entire range of ASCII symbols between the two around it. </summary>
        public const byte MatchRange = (byte)'-';


        /// <summary> Opens an array of strings in pattern matching. A match will occur if any of the strings within the array matches to a sequence of characters.  </summary>
        public const byte MatchStringArrayOpen = (byte)'{';

        /// <summary> Closes an array of strings in pattern matching. </summary>
        public const byte MatchStringArrayClose = (byte)'}';

        #endregion // PATTERN MATCHING CONSTS


        // consts for other special symbols specified in OSC protocol
        #region SPECIAL CHAR CONSTS

        /// <summary> Designates the start of an OSC Bundle. </summary>
        public const byte BundleMarker = (byte)'#';

        /// <summary> Separates parts of an address string inside OSC Messages. Always should be present at the start of an address string. </summary>
        public const byte Separator = (byte)'/';

        /// <summary> Designates the beginning of an OSC type tag string inside messages. Separates strings in string arrays when pattern matching. </summary>
        public const byte Comma = (byte)',';

        /// <summary> Personal space. Not allowed in OSC Method or Container names, otherwise insignificant. </summary>
        public const byte Space = (byte)' ';

        #endregion // SPECIAL SYMBOL CONSTS

        // reserved characters that shouldn't be used in osc method or container names - just to have them all in nice arrays
        private static readonly byte[] _specialChars = new byte[] { Space, BundleMarker, Comma, Separator };

        private static readonly byte[] _patternMatchChars = new byte[] { Comma, MatchAnySequence, MatchAnyChar, MatchNot, MatchRange, 
                                                                           MatchCharArrayOpen, MatchCharArrayClose, MatchStringArrayOpen, MatchStringArrayClose };



        /// <summary>
        /// Checks whether the provided byte represents an ASCII character reserved by the OSC Protocol.
        /// </summary>
        public static bool IsSpecialChar(byte asciiChar)
        {
            for (int i = 0; i < _specialChars.Length; i++)
            {
                if (asciiChar == _specialChars[i])
                    return true;
            }

            return false;

        }


        /// <summary>
        /// Checks whether the provided byte represents an ASCII character used in the OSC Protocol-specified pattern-matching.
        /// </summary>
        public static bool IsPatternMatchChar(byte asciiChar)
        {
            for (int i = 0; i < _patternMatchChars.Length; i++)
            {
                if (asciiChar == _patternMatchChars[i])
                    return true;
            }

            return false;
        }


        /// <summary>
        /// Checks whether the provided byte array contains ASCII characters reserved by the OSC Protocol.
        /// </summary>
        public static bool ContainsOscSpecialChars(this byte[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                if (IsSpecialChar(array[i]))
                {
                    return true;
                }

            }

            return false;

        }


        /// <summary>
        /// Checks whether the provided byte array contains ASCII characters used in the OSC Protocol-specified pattern-matching.
        /// </summary>
        public static bool ContainsOscPatternMatching(this byte[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                if (IsPatternMatchChar(array[i]))
                {
                    return true;
                }

            }

            return false;

        }


        /// <summary>
        /// Performs very basic validation on a binary data array, trying to ensure that it actually contains OSC data (or at least something close enough to it).
        /// </summary>
        /// <param name="data"> The array to check. </param>
        /// <returns> "True" if the array seems to contain valid binary data (or at the very least adheres to the basic tenets of the OSC Protocol), "False" otherwise. </returns>
        public static bool IsValidOscData(this byte[] data)
        {
            if ((data[0] != BundleMarker) && (data[0] != Separator))
            {
                return false;
            }

            if (data.Length % 4 != 0)
            {
                return false;
            }

            return true;

        }


        /// <summary>
        /// Performs very basic validation on a binary data array, trying to ensure that the specified part of it actually contains OSC data (or at least something close enough to it).
        /// </summary>
        /// <param name="data"> The array to check. </param>
        /// <param name="index"> The index to check from. </param>
        /// <param name="length"> The total number of bytes to check. </param>
        /// <returns> "True" if the array seems to contain valid binary data (or at the very least adheres to the basic tenets of the OSC Protocol), "False" otherwise. </returns>
        public static bool IsValidOscData(this byte[] data, int index, int length)
        {
            if ((data[index] != BundleMarker) && (data[index] != Separator))
            {
                return false;
            }

            if (length % 4 != 0)
            {
                return false;
            }

            // duh
            if (index + length > (data.Length))
            {
                return false;
            }

            return true;

        }

    }

}
