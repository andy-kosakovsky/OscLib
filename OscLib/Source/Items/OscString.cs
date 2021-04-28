using System;
using System.Text;

namespace OscLib
{

    /// <summary>
    /// Implements an OSC Protocol-compliant string - ASCII-based, null-terminated, length a multiple of 4 - that can be used with and easily converted to and from standard .NET strings.  
    /// </summary>
    public struct OscString
    {
        private static readonly OscString _nullString = new OscString("\0");

        /// <summary> Returns a null OscString - that is, an OSC string containing a single null symbol. </summary>
        public static OscString NullString
        {
            get
            {
                return _nullString;
            }

        }

        private readonly byte[] _chars;

        public readonly int OscLength;
        public readonly int Length;

        private Trit _containsPatternMatching;
        private Trit _containsSpecialSymbols;
   
        /// <summary>
        /// Indexer access to the characters of this string, as ASCII codes.
        /// </summary>
        /// <param name="i"> Index of the char. </param>
        /// <returns> A char as an ASCII code. Will return "0" (ASCII null) when out of range. </returns>
        public byte this[int i] 
        {
            get
            {
                if ((i >= 0) && (i < _chars.Length))
                {
                    return _chars[i];
                }
                else
                {
                    return 0;
                }

            }

        }


        /// <summary>
        /// Creates an OSC String out of a byte array.
        /// </summary>
        /// <param name="bytes"> The source byte array. </param>
        public OscString(byte[] bytes)
        {
            _chars = bytes;
            OscLength = OscUtil.GetNextMultipleOfFour(_chars.Length);
            Length = _chars.Length;

            _containsPatternMatching = Trit.Maybe;
            _containsSpecialSymbols = Trit.Maybe;
        }


        /// <summary>
        /// Creates an OSC String out of a part of a byte array.
        /// </summary>
        /// <param name="bytes"> The source byte array. </param>
        /// <param name="start"> The start index. </param>
        /// <param name="length"> The length of the string. </param>
        public OscString(byte[] bytes, int start, int length)
        {
            _chars = new byte[length];
            Array.Copy(bytes, start, _chars, 0, length);

            OscLength = OscUtil.GetNextMultipleOfFour(_chars.Length);
            Length = _chars.Length;

            _containsPatternMatching = Trit.Maybe;
            _containsSpecialSymbols = Trit.Maybe;
        }


        /// <summary>
        /// Creates an OSC String out of a plain old string.
        /// </summary>
        /// <param name="sourceString"> The source plain old string. </param>
        public OscString(string sourceString)
        {
            _chars = Encoding.ASCII.GetBytes(sourceString);

            OscLength = OscUtil.GetNextMultipleOfFour(_chars.Length);
            Length = _chars.Length;

            _containsPatternMatching = Trit.Maybe;
            _containsSpecialSymbols = Trit.Maybe;
        }


        /// <summary>
        /// Internal constructor for copying strings while preserving the information about them containing special symbols.
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="specialSymbols"></param>
        private OscString(byte[] bytes, Trit hasSpecialSymbols, Trit hasPatternMatching)
        {
            _chars = bytes;
            OscLength = OscUtil.GetNextMultipleOfFour(_chars.Length);
            Length = _chars.Length;

            _containsPatternMatching = hasPatternMatching;
            _containsSpecialSymbols = hasSpecialSymbols;
        }


        /// <summary>
        /// Copies the elements of this string's byte array into the specified one-dimentional byte array, starting from the index provided.
        /// </summary>
        /// <returns></returns>
        public void CopyTo(byte[] array, int index)
        {
            _chars.CopyTo(array, index);
        }


        /// <summary>
        /// Returns a copy of this OSC string.
        /// </summary>
        /// <returns></returns>
        public OscString Copy()
        { 
            return new OscString(_chars, _containsSpecialSymbols, _containsPatternMatching);
        }


        /// <summary>
        /// Splits the OSC String into an array of new OSC Strings, using the provided symbol.
        /// </summary>
        /// <param name="splitByte"> The ASCII encoding of a symbol by which to split the string. </param>
        /// <returns> An array of resulting OSC Strings. </returns>
        public OscString[] Split(byte splitByte)
        {
            OscString[] result;

            int substringStartIndex = 0;
            int substringLength = 0;
            int substringIndex = 0;
            int substringTotal = 1;

            // get the amount of splits
            for (int i = 0; i < _chars.Length; i++)
            {
                if ((_chars[i] == splitByte) && (i > 0) && (i < _chars.Length - 1))
                {
                    substringTotal++;
                }

            }

            result = new OscString[substringTotal];

            for (int i = 0; i < _chars.Length; i++)
            {
                if (_chars[i] == splitByte)
                {
                    if (substringLength > 0)
                    {
                        result[substringIndex] = new OscString(_chars, substringStartIndex, substringLength);
                        substringIndex++;
                    }

                    substringStartIndex = i + 1;
                    substringLength = 0;

                }
                else
                {
                    substringLength++;

                    if (i == _chars.Length - 1)
                    {
                        result[substringIndex] = new OscString(_chars, substringStartIndex, substringLength);
                        substringIndex++;
                    }

                }

            }

            return result;

        }


        /// <summary> 
        /// Returns a copy of an array containing all characters (their ASCII codes as bytes, that is) in this string. 
        /// </summary>
        public byte[] GetChars()
        {
            byte[] copy = new byte[Length];
            _chars.CopyTo(copy, 0);
            return copy;
        }


        /// <summary>
        /// Returns a copy of an array containing all chars (their ASCII codes as bytes, that is). 
        /// </summary>
        public byte[] GetOscBytes()
        {
            byte[] copy = new byte[OscLength];
            _chars.CopyTo(copy, 0);
            return copy;
        }


        /// <summary>
        /// Gets a substring from this string.
        /// </summary>
        /// <param name="start"> The index at which the substring starts. </param>
        /// <param name="length"> The length of the substring. </param>
        /// <returns> The requested substring. </returns>
        /// <exception cref="ArgumentException"> Thrown when "start" is beyond the string's length, or when "length" is too large. </exception>
        public OscString GetSubstring(int start, int length)
        {
            if ((start >= Length) || (start < 0))
                throw new ArgumentException("OSC String ERROR: Cannot get a substring, it starts beyond the length of the string.");

            if (start + length > Length)
                throw new ArgumentException("Osc String ERROR: Cannot get a substring, it's too long");

            return new OscString(_chars, start, length);
        }


        /// <summary>
        /// Compares this string to a pattern, returns whether they match or not.
        /// </summary>
        /// <param name="pattern"> The pattern to which the string is compared. </param>
        /// <returns> True if string matches the pattern, false if not (duh). </returns>
        /// <exception cref="ArgumentException"> Thrown when either the curly brackets or the square brackets aren't closed in the pattern. </exception>
        public bool PatternMatch(OscString pattern)
        {
            // check if this string is eligible for pattern-matching (eg. it's not a pattern itself)
            if (ContainsPatternMatching())
            {
                throw new ArgumentException("OSC String ERROR: Can't pattern-match two patterns");
            }
            
            // first, let's cover some common situations
            // if pattern consists of only one "*" symbol then it'll match to anything
            if ((pattern._chars.Length == 1) && (pattern[0] == OscProtocol.MatchAnySequence))
            {               
                return true;
            }

            int patIndex = 0, strIndex = 0;

            // revert locations
            int patRevert = -1, strRevert = -1;

            while (strIndex < this.Length)
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
                if (pattern[patIndex] == OscProtocol.MatchAnySequence)
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
                    if (!CharMatchesSquareBrackets(this[strIndex], ref patIndex, ref pattern))
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
                    if (!StringMatchesCurlyBrackets(ref pattern, ref strIndex, ref patIndex))
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
                else if (!CharIsEqual(this[strIndex], pattern[patIndex]))
                {
                    // if we don't have something to return to
                    if (patRevert < 0)
                    {
                        return false;
                    }

                    strIndex = ++strRevert;
                    patIndex = patRevert;

                    // if the place where the string will be reverted reaches beyond the length of the string, that means string doesn't fit the pattern
                    if (strRevert >= Length)
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

            while ((patIndex < pattern.Length) && (pattern[patIndex] == OscProtocol.MatchAnySequence))
            {
                patIndex++;
            }

            return (patIndex == pattern.Length);

        }


        /// <summary>
        /// Whether this string contains special symbols reserved by OSC protocol. Checks when first called (can get quite expensive, depending on the length of the string), then caches the result.
        /// </summary>
        public bool ContainsSpecialSymbols()
        {       
            if (_containsSpecialSymbols == Trit.Maybe)
            {
                // let's find out, shall we
                bool contains = _chars.ContainsOscSpecialSymbols();

                _containsSpecialSymbols = contains.ToTrit();
                return contains;

            }
            else if (_containsSpecialSymbols == Trit.True)
            {
                return true;
            }
            else
            {
                return false;
            }

        }

        /// <summary>
        /// Whether this string contains any pattern-matching symbols reserved by OSC protocol. Checks when first called (can get quite expensive, depending on the length of the string), then caches the result.
        /// </summary>
        public bool ContainsPatternMatching()
        {
            if (_containsPatternMatching == Trit.Maybe)
            {
                // let's find out, shall we
                bool contains = _chars.ContainsOscPatternMatching();

                _containsPatternMatching = contains.ToTrit();
                return contains;

            }
            else if (_containsPatternMatching == Trit.True)
            {
                return true;
            }
            else
            {
                return false;
            }

        }


        public void SetSpecialSymbols(bool value)
        {
            _containsSpecialSymbols = value.ToTrit();
        }


        public void SetPatternMatching(bool value)
        {
            _containsPatternMatching = value.ToTrit();
        }


        #region OVERRIDES AND OPERATORS

        /// <summary>
        /// Returns the OSC string as an actual string.
        /// </summary>
        /// <returns> A string of a string. </returns>
        public override string ToString()
        {
            return Encoding.ASCII.GetString(_chars);
        }

        /// <summary>
        /// Compares the OSC string to an object.
        /// </summary>
        /// <param name="obj"> Object to compare to. </param>
        /// <returns> Whether the OSC string is equal to object. </returns>
        public override bool Equals(object obj)
        {
            if (obj is OscString oscString)
            {
                return (this == oscString);
            }
            else if (obj is string stringString)
            {
                return (this == stringString);
            }
            else
                return false;

        }

        /// <summary>
        /// Returns a hash code of the OSC string, so it can be used as a key in dictionaries, for example.
        /// </summary>
        /// <returns> A hash code. </returns>
        public override int GetHashCode()
        {
            unchecked
            {
                
                if (_chars == null)
                    return 0;

                if (_chars.Length == 0)
                    return 0;

                // polynomial
                int prime = 251;
                int hash = 0;

                for (int i = 0; i < _chars.Length; i++)
                {
                    hash += _chars[i] * prime;
                    prime *= prime;
                }

                // all my homies use magic numbers 
                return hash % 1566083941;
            }

        }


        // used in pattern matching - will return true if char is compared to itself or a special symbol such as * or ?
        


        /// <summary>
        /// Allows to compare OSC strings to each other.
        /// </summary>
        /// <param name="stringOne"> First string. </param>
        /// <param name="stringTwo"> Second string. </param>
        /// <returns> Whether the strings are equal. </returns>        
        public static bool operator ==(OscString stringOne, OscString stringTwo)
        {

            if (stringOne.Length != stringTwo.Length)
                return false;

            for (int i = 0; i < stringOne.Length; i++)
            {
                if (stringOne[i] != stringTwo[i])
                    return false;
            }

            return true;

        }

        /// <summary>
        /// Allows to compare OSC strings to each other. 
        /// </summary>
        /// <param name="stringOne"> First string. </param>
        /// <param name="stringTwo"> Second string. </param>
        /// <returns> Whether the strings are unequal. </returns>
        public static bool operator !=(OscString stringOne, OscString stringTwo)
        {
            if (stringOne.Length != stringTwo.Length)
                return true;


            for (int i = 0; i < stringOne.Length; i++)
            {
                if (stringOne[i] != stringTwo[i])
                    return true;
            }

            return false;

        }


        /// <summary>
        /// Allows to implicitly convert strings to OscStrings.
        /// </summary>
        /// <param name="addressString"> String to be converted. </param>
        public static implicit operator OscString(string addressString)
        {
            return new OscString(addressString);
        }


        /// <summary>
        /// Allows to explicitly convert OscStrings to strings.
        /// </summary>
        /// <param name="oscString"> OscString to be converted. </param>
        public static explicit operator string(OscString oscString)
        {
            return oscString.ToString();
        }

        #endregion // OVERRIDES AND OPERATORS


        /// <summary>
        /// Returns true if the provided OscString is null (only contains null symbols) or empty (doesn't contain any symbols).
        /// </summary>
        /// <param name="checkMe"> The OscString to be checked. </param>
        /// <returns></returns>
        public static bool IsNullOrEmpty(OscString checkMe)
        {
            if (checkMe.Length < 1)
            {
                return true;
            }

            for (int i = 0; i < checkMe.Length; i++)
            {
                if (checkMe[i] != '\0')
                {
                    return false;
                }
            }

            return true;

        }

        private bool CharMatchesSquareBrackets(byte checkChar, ref int pointer, ref OscString pattern)
        {
            // find the end
            int bracketEnd = -1;

            int bracketStart = pointer;

            bool reverse = false;
            bool found = false;

            while (pointer < pattern.Length)
            {
                if (pattern[pointer] == OscProtocol.MatchCharArrayClose)
                {
                    bracketEnd = pointer;
                    // make sure the pointer is going past the bracket
                    pointer++;
                    break;

                }

                if (!found)
                {

                    if (pattern[pointer] == OscProtocol.MatchNot)
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
                        if ((pointer > bracketStart + 1) && (((pointer + 1) < pattern.Length) && (pattern[pointer + 1] != OscProtocol.MatchCharArrayClose)))
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

        private bool StringMatchesCurlyBrackets(ref OscString pattern, ref int strPointer, ref int patPointer)
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

                    if (pattern[patPointer] == OscProtocol.MatchStringArrayClose)
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
                        if (strPointer < this.Length)
                        {
                            if (!CharIsEqual(this[strPointer], pattern[patPointer]))
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

        private bool CharIsEqual(byte strChar, byte patChar)
        {
            if (patChar == OscProtocol.MatchAnyChar)
            {
                return true;
            }
            else
            {
                return strChar == patChar;
            }

        }


    }

}
