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
        /// <summary> Length in bytes of a single OSC data chunk. </summary>
        public const int SingleChunk = 4;
        /// <summary> Length in bytes of a double-sized OSC data chunk. </summary>
        public const int DoubleChunk = 8;

        // consts for type tags that are recognized by OSC
        /// <summary> OSC type tag for Integer data type. </summary>
        public const byte TypeTagInteger = (byte)'i';

        /// <summary> OSC type tag for Float data type. </summary>
        public const byte TypeTagFloat = (byte)'f';

        /// <summary> OSC type tag for String data type. </summary>
        public const byte TypeTagString = (byte)'s';

        /// <summary> OSC type tag for Blob data type. </summary>
        public const byte TypeTagBlob = (byte)'b';

        /// <summary> OSC type tag for Timestamp data type. </summary>
        public const byte TypeTagTimestamp = (byte)'t';

        /// <summary> OSC type tag for Double data type. </summary>
        public const byte TypeTagDouble = (byte)'d';

        /// <summary> OSC type tag for Long data type. </summary>
        public const byte TypeTagLong = (byte)'h';

        // consts for other special symbols specified in OSC protocol
        /// <summary> Designates start of a bundle. </summary>
        public const byte SymbolBundleStart = (byte)'#';

        /// <summary> Separates parts of address string. Always should present at the start of an address string. </summary>
        public const byte SymbolAddressSeparator = (byte)'/';

        /// <summary> Designates the beginning of an OSC type tag string, separates substrings in pattern matching. </summary>
        public const byte SymbolComma = (byte)',';

        /// <summary> Personal space. </summary>
        public const byte SymbolSpace = (byte)' ';

        /// <summary> A wildcard symbol in pattern matching. </summary>
        public const byte SymbolAsterisk = (byte)'*';

        /// <summary> An "any symbol" symbol in pattern matching. </summary>
        public const byte SymbolQuestion = (byte)'?';

        /// <summary> A reversal symbol in pattern matching.  </summary>
        public const byte SymbolExclamation = (byte)'!';

        /// <summary> A range symbol in pattern matching. </summary>
        public const byte SymbolDash = (byte)'-';

        /// <summary> Square brackets used in pattern matching. </summary>
        public const byte SymbolOpenSquare = (byte)'[';

        /// <summary> Square brackets used in pattern matching. </summary>
        public const byte SymbolClosedSquare = (byte)']';

        /// <summary> Curly brackets used in pattern matching. </summary>
        public const byte SymbolOpenCurly = (byte)'{';

        /// <summary> Curly brackets used in pattern matching. </summary>
        public const byte SymbolClosedCurly = (byte)'}';

        // bundles are marked by having "#bundle" as their address string. this is a "prerendered" bundle message
        private static readonly byte[] _bundleDesignator;

        // reserved symbols that shouldn't be used in osc method or container names - just to have them all in a nice container
        private static readonly byte[] _addressStringSpecialSymbols;

        private static readonly IPAddress _localIP;

        /// <summary> The byte values for the "#bundle " string that designates an OSC bundle (duh). Pls don't change elements, thx. </summary>
        public static byte[] BundleDesignator { get => _bundleDesignator; }

        /// <summary> Reserved symbols that shouldn't be used in OSC Method or Container names - just to have all of them in a nice and tidy array. Pls don't change elements, thx. </summary>
        public static byte[] AddressStringSpecialSymbols { get => _addressStringSpecialSymbols; }

        /// <summary> Local IP address. </summary>
        public static IPAddress LocalIP { get => _localIP; }

        static OscProtocol()
        {
            _bundleDesignator = new byte[8] { SymbolBundleStart, (byte)'b', (byte)'u', (byte)'n', (byte)'d', (byte)'l', (byte)'e', 0 };

            _addressStringSpecialSymbols = new byte[] { SymbolSpace, SymbolBundleStart, SymbolAsterisk,
            SymbolComma, SymbolAddressSeparator, SymbolQuestion, SymbolExclamation, SymbolDash, SymbolOpenSquare, SymbolClosedSquare, SymbolOpenCurly, SymbolClosedCurly };

            _localIP = IPAddress.Parse("127.0.0.1");

        }

    }

}
