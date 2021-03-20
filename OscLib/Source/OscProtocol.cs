using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace OscLib
{

    /// <summary>
    /// Describes the OSC protocol convention on type tags and bundle designators.
    /// </summary>
    public static class OscProtocol
    {
        // byte lengths of data chunks used by OSC

        /// <summary> Length in bytes of a single (32 bits/4 bytes long) OSC data chunk. </summary>
        public const int Chunk32 = 4;
        /// <summary> Length in bytes of a double-sized (64 bits/8 bytes long) OSC data chunk. </summary>
        public const int Chunk64 = 8;

        // consts for type tags that are recognized by OSC
        #region TYPE TAG CONSTS

        /// <summary> OSC type tag for Int32 data type. </summary>
        public const byte TypeTagInt32 = (byte)'i';

        /// <summary> OSC type tag for Float32 data type. </summary>
        public const byte TypeTagFloat32 = (byte)'f';

        /// <summary> OSC type tag for Int64/Long data type. </summary>
        public const byte TypeTagInt64 = (byte)'h';

        /// <summary> OSC type tag for Float64/Double data type. </summary>
        public const byte TypeTagFloat64 = (byte)'d';

        /// <summary> OSC type tag for String data type. </summary>
        public const byte TypeTagString = (byte)'s';

        /// <summary> OSC type tag for Blob data type. </summary>
        public const byte TypeTagBlob = (byte)'b';

        /// <summary> OSC type tag for OSC Time Tag data type. </summary>
        public const byte TypeTagTime = (byte)'t';

        #endregion // TYPE TAG CONSTS


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


        #region PATTERN MATCHING CONSTS

        /// <summary> Stands for any sequence of zero or more characters in pattern matching. </summary>
        public const byte MatchAnySeqence = (byte)'*';

        /// <summary> Stands for any single character in pattern matching. </summary>
        public const byte MatchAnyChar = (byte)'?';


        /// <summary> Opens an array of characters in pattern matching. A match will occur if any of the characters within the array corresponds to a single character. </summary>
        public const byte MatchCharArrayOpen = (byte)'[';

        /// <summary> Closes an array of characters in pattern matching. </summary>
        public const byte MatchCharArrayClose = (byte)']';

        /// <summary> "Reverses" the character array, matching with any symbol *not* present in it.  </summary>
        public const byte MatchNot = (byte)'!';

        /// <summary> A range symbol used inside . </summary>
        public const byte MatchRange = (byte)'-';

       
        /// <summary> Opens an array of strings in pattern matching. A match will occur if any of the strings within the array matches to a sequence of characters.  </summary>
        public const byte MatchStringArrayOpen = (byte)'{';

        /// <summary> Closes an array of strings in pattern matching. </summary>
        public const byte MatchStringArrayClose = (byte)'}';

        #endregion // PATTERN MATCHING CONSTS


        // bundles are marked by having "#bundle" as their address string. this is a "prerendered" bundle message
        private static readonly byte[] _bundleString;

        private static readonly int _bundleStringLength;

        // reserved symbols that shouldn't be used in osc method or container names - just to have them all in a nice container
        private static readonly byte[] _addressReservedSymbols;


        private static readonly IPAddress _localIP;



        /// <summary> Local IP address. </summary>
        public static IPAddress LocalIP { get => _localIP; }

        /// <summary> Cached length of the "#bundle " string. </summary>
        public static int BundleStringLength { get => _bundleStringLength; }

        static OscProtocol()
        {
            _bundleString = new byte[8] { BundleMarker, (byte)'b', (byte)'u', (byte)'n', (byte)'d', (byte)'l', (byte)'e', 0 };
            _bundleStringLength = _bundleString.Length;

            _addressReservedSymbols = new byte[] { Space, BundleMarker, MatchAnySeqence,
            Comma, Separator, MatchAnyChar, MatchNot, MatchRange, MatchCharArrayOpen, MatchCharArrayClose, MatchStringArrayOpen, MatchStringArrayClose };

            _localIP = IPAddress.Parse("127.0.0.1");

        }

        /// <summary>
        /// Checks whether the provided byte represents an ASCII symbol reserved by the OSC Protocol.
        /// </summary>
        /// <param name="symbol"> ASCII symbol as a byte. </param>
        /// <returns></returns>
        public static bool IsAReservedSymbol(byte symbol)
        {
            for (int i = 0; i < _addressReservedSymbols.Length; i++)
            {
                if (symbol == _addressReservedSymbols[i])
                    return true;
            }

            return false;

        }


        /// <summary>
        /// Checks whether the byte array contains any ASCII symbols reserved by the OSC Protocol.
        /// </summary>
        /// <param name="array"> An array presumably containing ASCII symbols as bytes. </param>
        /// <returns></returns>
        public static bool ContainsReservedSymbols(byte[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                if (IsAReservedSymbol(array[i]))
                {
                    return true;
                }

            }

            return false;
        }


        /// <summary>
        /// Copies the bundle string ("#bundle ") to array, starting at the specified index.
        /// </summary>
        /// <param name="array"> Target array. </param>
        /// <param name="index"> Target index. </param>
        public static void CopyBundleStringTo(byte[] array, int index)
        {
            _bundleString.CopyTo(array, index);
        }

    }

}
