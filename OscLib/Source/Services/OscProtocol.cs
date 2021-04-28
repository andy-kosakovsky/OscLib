using System;
using System.Collections.Generic;
using System.Text;

namespace OscLib
{
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
        #region SPECIAL SYMBOL CONSTS

        /// <summary> Designates the start of an OSC Bundle. </summary>
        public const byte BundleMarker = (byte)'#';

        /// <summary> Separates parts of an address string inside OSC Messages. Always should be present at the start of an address string. </summary>
        public const byte Separator = (byte)'/';

        /// <summary> Designates the beginning of an OSC type tag string inside messages. Separates strings in string arrays when pattern matching. </summary>
        public const byte Comma = (byte)',';

        /// <summary> Personal space. Not allowed in OSC Method or Container names, otherwise insignificant. </summary>
        public const byte Space = (byte)' ';

        #endregion // SPECIAL SYMBOL CONSTS

        // reserved symbols that shouldn't be used in osc method or container names - just to have them all in nice containers
        private static readonly byte[] _specialSymbols = new byte[] { Space, BundleMarker, Comma, Separator };

        private static readonly byte[] _patternMatchSymbols = new byte[] { Comma, MatchAnySequence, MatchAnyChar, MatchNot, MatchRange, 
                                                                           MatchCharArrayOpen, MatchCharArrayClose, MatchStringArrayOpen, MatchStringArrayClose };



        /// <summary>
        /// Checks whether the provided byte represents an ASCII symbol reserved by the OSC Protocol.
        /// </summary>
        /// <param name="symbol"> ASCII symbol as a byte. </param>
        /// <returns></returns>
        public static bool IsSpecialSymbol(byte symbol)
        {
            for (int i = 0; i < _specialSymbols.Length; i++)
            {
                if (symbol == _specialSymbols[i])
                    return true;
            }

            return false;

        }


        public static bool IsPatternMatchSymbol(byte symbol)
        {
            for (int i = 0; i < _patternMatchSymbols.Length; i++)
            {
                if (symbol == _patternMatchSymbols[i])
                    return true;
            }

            return false;
        }


        public static bool ContainsOscSpecialSymbols(this byte[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                if (IsSpecialSymbol(array[i]))
                {
                    return true;
                }

            }

            return false;

        }


        public static bool ContainsOscPatternMatching(this byte[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                if (IsPatternMatchSymbol(array[i]))
                {
                    return true;
                }

            }

            return false;

        }

    }

}
