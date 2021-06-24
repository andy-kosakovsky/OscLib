using System;

namespace OscLib
{
    /// <summary>
    /// Contains common constants, values and symbols, as well as pattern-matching and data-validation functionality as defined by the Open Sound Control Protocol specification.
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


        #region CHARACTER CHECKS
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

        #endregion // CHARACTER CHECKS


        #region DATA VALIDATION
        /// <summary>
        /// Performs very basic validation on a binary data array, trying to ensure that it actually contains OSC data (or at least something close enough to it).
        /// </summary>
        /// <param name="data"> The array to check. </param>
        /// <returns> "True" if the array seems to contain valid binary data (or at the very least adheres to the basic tenets of the OSC Protocol), "False" otherwise. </returns>
        public static bool IsValidOscData(this byte[] data)
        {
            if (CheckOscContents(data) == PacketContents.BadData)
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
            if (CheckOscContents(data, index) == PacketContents.BadData)
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

        #endregion // DATA VALIDATION


        #region PACKET CONTENTS CHECKS
        /// <summary>
        /// Checks whether the provided byte array might contain an OSC Message or a Bundle.
        /// </summary>
        /// <param name="data"> The array to check. </param>
        /// <param name="index"> The index to check (0 by default). </param>
        public static PacketContents CheckOscContents(this byte[] data, int index = 0)
        {
            if (data[index] == Separator)
            {
                return PacketContents.Message;
            }

            if (data[index] == BundleMarker)
            {
                return PacketContents.Bundle;
            }

            return PacketContents.BadData;
        }


        /// <summary>
        /// Checks whether the provided OSC Packet contains an OSC Message or a Bundle.
        /// </summary>
        /// <typeparam name="TPacket"> Should implement the IOscPacket interface. </typeparam>
        /// <param name="packet"> The packet to check. </param>
        /// <param name="index"> The index to check (0 by default). </param>
        public static PacketContents CheckOscContents<TPacket>(this TPacket packet, int index = 0) where TPacket : IOscPacket
        {
            return CheckOscContents(packet.GetBytes(), index);
        }

        #endregion // PACKET CONTENTS CHECKS


        #region PATTERN-MATCHING
        /// <summary>
        /// Compares a string to the specified pattern, returns whether they match or not.
        /// </summary>
        /// <remarks> If the string in question is a pattern itself, this method will return "false" unless the patterns are identical. </remarks>
        /// <param name="pattern"> The pattern to which the string is compared. </param>
        /// <returns> "True" if string matches the pattern, "false" if not (duh). </returns>
        /// <exception cref="ArgumentException"> Thrown when either the curly brackets or the square brackets in the pattern aren't closed. </exception>
        public static bool PatternMatch(this OscString me, OscString pattern)
        {
            // check if this string is eligible for pattern-matching (eg. it's not a pattern itself)
            if (me.ContainsPatternMatching())
            {
                return me == pattern;
            }

            // first, let's cover some common situations
            // if pattern consists of only one "*" symbol then it'll match to anything
            if ((pattern.Length == 1) && (pattern[0] == MatchAnySequence))
            {
                return true;
            }

            int patIndex = 0, strIndex = 0;

            // revert locations
            int patRevert = -1, strRevert = -1;

            while (strIndex < me.Length)
            {
                // overflow protection
                if (patIndex >= pattern.Length)
                {
                    if (patRevert < 0)
                    {
                        return false;
                    }

                    patIndex = patRevert;
                }


                // check for '*'
                if (pattern[patIndex] == MatchAnySequence)
                {

                    patRevert = ++patIndex;
                    strRevert = strIndex;

                    // in case "*" is the last char in the pattern
                    if (patIndex >= pattern.Length)
                    {
                        return true;
                    }
                    
                }
                // check for []
                else if (pattern[patIndex] == OscProtocol.MatchCharArrayOpen)
                {
                    if (!CharMatchesSquareBrackets(me[strIndex], ref patIndex, ref pattern))
                    {
                        // if we don't have something to return to
                        if (patRevert < 0)
                        {

                            return false;
                        }

                        strIndex = ++strRevert;
                        patIndex = patRevert;

                    }
                    else
                    {
                        strIndex++;
                    }
                }
                // check for {}
                else if (pattern[patIndex] == OscProtocol.MatchStringArrayOpen)
                {
                    if (!me.StringMatchesCurlyBrackets(ref pattern, ref strIndex, ref patIndex))
                    {
                        // if we don't have something to return to
                        if (patRevert < 0)
                        {
                            return false;
                        }

                        strIndex = ++strRevert;
                        patIndex = patRevert;

                    }

                }
                // check if unequal
                else if (!CharIsEqual(me[strIndex], pattern[patIndex]))
                {
                    // if we don't have something to return to
                    if (patRevert < 0)
                    {
                        return false;
                    }

                    strIndex = ++strRevert;
                    patIndex = patRevert;

                    // if the place where the string will be reverted reaches beyond the length of the string, that means string doesn't fit the pattern
                    if (strRevert >= me.Length)
                    {
                        return false;
                    }

                }
                else
                {
                    strIndex++;
                    patIndex++;
                }

            }

            while ((patIndex < pattern.Length) && (pattern[patIndex] == MatchAnySequence))
            {
                patIndex++;
            }

            return (patIndex == pattern.Length);

        }


        private static bool CharMatchesSquareBrackets(byte checkChar, ref int pointer, ref OscString pattern)
        {
            // find the end
            int bracketEnd = -1;

            int bracketStart = pointer;

            bool reverse = false;
            bool found = false;

            while (pointer < pattern.Length)
            {
                if (pattern[pointer] == MatchCharArrayClose)
                {
                    bracketEnd = pointer;
                    // make sure the pointer is going past the bracket
                    pointer++;
                    break;

                }

                if (!found)
                {

                    if (pattern[pointer] == MatchNot)
                    {
                        // if it's at the beginning, make sure that it's noted, and keep a space for it in the return array 
                        if (bracketStart == (pointer - 1))
                        {
                            reverse = true;
                        }

                    }
                    else if (pattern[pointer] == OscProtocol.MatchRange)
                    {
                        // if we're not at the start, and if we're not by the end of the char array, so we can safely check back and forth
                        if ((pointer > bracketStart + 1) && (((pointer + 1) < pattern.Length) && (pattern[pointer + 1] != MatchCharArrayClose)))
                        {
                            if (OscUtil.IsNumberBetween(checkChar, pattern[pointer - 1], pattern[pointer + 1]))
                            {
                                found = true;
                            }

                        }

                    }
                    else
                    {
                        // if it's any other, non-special symbol, let's just compare it for now
                        if (CharIsEqual(checkChar, pattern[pointer]))
                        {
                            found = true;
                        }

                    }

                }

                pointer++;

            }

            // if we didn't find the bracket end, something is wrong with the string
            if (bracketEnd < 0)
                throw new ArgumentException("Pattern Match ERROR: pattern syntax error, square bracket opened at " + bracketStart + " is not closed");

            // if we got this far, that means the char we're checking for doesn't occur inside the brackets - we can return the value of "reverse"

            return reverse ^ found;

        }


        private static bool StringMatchesCurlyBrackets(this OscString me, ref OscString pattern, ref int strPointer, ref int patPointer)
        {
            // TODO: this will do for now, can be redone into something more efficient later. also, add support for special symbols within the string

            // get start and end of curly brackets, and the total of strings
            int curlyStart = patPointer, curlyEnd = -1;
            bool found = false, substringFits = true;

            int inputStringStart = strPointer;

            // shift patpointer forwards once
            patPointer++;


            while (patPointer < pattern.Length)
            {
                if ((pattern[patPointer] == OscProtocol.Comma) || (pattern[patPointer] == OscProtocol.MatchStringArrayClose))
                {
                    if (substringFits)
                    {
                        found = true;
                    }
                    else
                    {
                        strPointer = inputStringStart;
                    }

                    substringFits = true;

                    if (pattern[patPointer] == MatchStringArrayClose)
                    {
                        curlyEnd = patPointer;
                        patPointer++;
                        break;
                    }

                    patPointer++;

                }
                else
                {
                    if ((!found) && (substringFits))
                    {
                        if (strPointer < me.Length)
                        {
                            if (!CharIsEqual(me[strPointer], pattern[patPointer]))
                            {
                                substringFits = false;
                            }
                        }
                        else
                        {
                            substringFits = false;
                        }

                        strPointer++;
                    }

                    patPointer++;

                }

            }

            if (curlyEnd < 0)
                throw new ArgumentException("Pattern Match ERROR: pattern syntax error, curly bracket opened at " + curlyStart + " is not closed");

            return found;

        }


        private static bool CharIsEqual(byte strChar, byte patChar)
        {
            if (patChar == MatchAnyChar)
            {
                return true;
            }
            else
            {
                return strChar == patChar;
            }

        }

        #endregion // PATTERN-MATCHING EXTRAS
    }

}
