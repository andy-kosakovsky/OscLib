using System;
using System.Text;

namespace OscLib
{
    /// <summary>
    /// Implements an OSC Protocol-compliant string (ASCII-based, null-terminated, length a multiple of 4) that can be used with and easily converted to and from standard .NET strings.  
    /// </summary>
    public struct OscString : IOscBinaryContainer
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


        /// <summary> Contains all characters making up this string, recorded as ASCII codes. </summary>
        private readonly byte[] _chars;

        /// <summary> The "OSC Protocol-compliant" length of this string - including the null terminator and extra null bytes at the end to make it a multiple of 4. </summary>
        public readonly int OscLength;

        private Trit _containsPatternMatching;
        private Trit _containsSpecialSymbols;

        /// <summary> Returns the total number of characters in this string. </summary>
        public int Length { get => _chars.Length; }
   
        /// <summary>
        /// Indexer access to the characters of this string, recorded as ASCII codes.
        /// </summary>
        /// <param name="index"> Character index. </param>
        /// <returns> A character as an ASCII code. Will return a zero (ASCII null, that is) when out of range. </returns>
        public byte this[int index] 
        {
            get
            {
                if ((index >= 0) && (index < _chars.Length))
                {
                    return _chars[index];
                }
                else
                {
                    return 0;
                }

            }

        }


        #region CONSTRUCTORS
        /// <summary>
        /// Creates an OSC String out of a byte array.
        /// </summary>
        /// <param name="bytes"> The source byte array. </param>
        public OscString(byte[] bytes)
        {
            _chars = bytes;
            OscLength = _chars.Length.NextX4();

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

            OscLength = _chars.Length.NextX4();

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

            OscLength = _chars.Length.NextX4();

            _containsPatternMatching = Trit.Maybe;
            _containsSpecialSymbols = Trit.Maybe;
        }


        /// <summary>
        /// Creats an OSC String from an array of OSC strings.
        /// </summary>
        /// <param name="strings"></param>
        public OscString(OscString[] strings)
        {
            _containsPatternMatching = Trit.Maybe;
            _containsSpecialSymbols = Trit.Maybe;

            int length = 0;

            for (int i = 0; i < strings.Length; i++)
            {
                length += strings[i].Length;

                // check trits while we're at it
                if ((strings[i]._containsPatternMatching == Trit.False) && (_containsPatternMatching != Trit.True))
                {
                    _containsPatternMatching = Trit.False;
                }
                else if (strings[i]._containsPatternMatching == Trit.True)
                {
                    _containsPatternMatching = Trit.True;
                }

                if ((strings[i]._containsSpecialSymbols == Trit.False) && (_containsSpecialSymbols != Trit.True))
                {
                    _containsSpecialSymbols = Trit.False;
                }
                else if (strings[i]._containsSpecialSymbols == Trit.True)
                {
                    _containsSpecialSymbols = Trit.True;
                }

            }

            byte[] data = new byte[length];

            int pointer = 0;

            for (int i = 0; i < strings.Length; i++)
            {
                strings[i].CopyBytesToArray(data, pointer);
                pointer += strings[i].Length;
            }

            _chars = data;
            OscLength = OscUtil.NextX4(data.Length);

        }


        /// <summary>
        /// Internal constructor for copying strings while preserving the information about them containing special symbols.
        /// </summary>
        private OscString(byte[] bytes, Trit hasSpecialSymbols, Trit hasPatternMatching)
        {
            _chars = bytes;
            OscLength = _chars.Length.NextX4();

            _containsPatternMatching = hasPatternMatching;
            _containsSpecialSymbols = hasSpecialSymbols;
        }

        #endregion // CONSTRUCTORS


        #region BINARY DATA ACCESS
        /// <summary>
        /// Retrieves the byte array containing all characters of this string.
        /// </summary>
        /// <remarks> Despite being read-only, it's still possible to modify individual elements of the array. If this behaviour is not preferable, using indexer accessor might be safer. </remarks>
        public byte[] GetBytes()
        {
            return _chars;
        }


        /// <summary>
        /// Returns a copy of the byte array containing all characters of this string.
        /// </summary>
        public byte[] GetCopyOfBytes()
        {
            byte[] copy = new byte[OscLength];
            _chars.CopyTo(copy, 0);
            return copy;
        }


        /// <summary>
        /// Copies all characters into the specified one-dimentional byte array, starting from the index provided.
        /// </summary>
        public void CopyBytesToArray(byte[] array, int index)
        {
            _chars.CopyTo(array, index);
        }

        #endregion // BINARY DATA ACCESS


        #region STRING MANIPULATION
        /// <summary>
        /// Returns a copy of this OSC String.
        /// </summary>
        public OscString Copy()
        {
            return new OscString(GetCopyOfBytes(), _containsSpecialSymbols, _containsPatternMatching);
        }


        /// <summary>
        /// Splits the OSC String into an array of new OSC Strings, using the provided symbol.
        /// </summary>
        /// <param name="splitByte"> The ASCII code of a symbol by which to split the string. </param>
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

        #endregion // STRING MANIPULATION


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


        #region SPECIAL CHAR CONTENT
        /// <summary>
        /// Whether this string contains special symbols reserved by OSC protocol. Checks when first called (can get quite expensive, depending on the length of the string), then caches the result.
        /// </summary>
        public bool ContainsSpecialChars()
        {       
            if (_containsSpecialSymbols == Trit.Maybe)
            {
                // let's find out, shall we
                bool contains = _chars.ContainsOscSpecialChars();

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


        /// <summary>
        /// Overrides the internal "has special symbols" flag, which might help avoiding the possibly-costy check later on.
        /// </summary>
        public void SetSpecialChars(bool value)
        {
            _containsSpecialSymbols = value.ToTrit();
        }


        /// <summary>
        /// Overrides the internal "has pattern-matching symbols" flag, which might help avoiding the possibly-costy check later on.
        /// </summary>
        public void SetPatternMatching(bool value)
        {
            _containsPatternMatching = value.ToTrit();
        }

        #endregion // SPECIAL CHAR CONTENT


        #region OVERRIDES AND OPERATORS
        /// <summary>
        /// Returns this OSC String as an actual string.
        /// </summary>
        /// <returns> A string of a string. </returns>
        public override string ToString()
        {
            return Encoding.ASCII.GetString(_chars);
        }


        /// <summary>
        /// Compares this OSC String to an object.
        /// </summary>
        /// <param name="obj"> Object to compare to. </param>
        /// <returns> Whether the OSC String is equal to the object. </returns>
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


        /// <summary>
        /// Allows to compare OSC Strings to each other.
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
        /// Allows to concatenate OSC Strings to one another.
        /// </summary>
        /// <param name="stringOne"> First string. </param>
        /// <param name="stringTwo"> Second string. </param>
        /// <returns> Both OSC Strings united together forever. </returns>
        public static OscString operator +(OscString stringOne, OscString stringTwo)
        {
            byte[] data = new byte[stringOne.Length + stringTwo.Length];

            stringOne.CopyBytesToArray(data, 0);

            stringTwo.CopyBytesToArray(data, stringOne.Length);

            return new OscString(data,
                TritUtil.Orish(stringOne._containsSpecialSymbols, stringTwo._containsSpecialSymbols),
                TritUtil.Orish(stringOne._containsPatternMatching, stringTwo._containsPatternMatching)
                );

        }


        /// <summary>
        /// Allows to concatenate an OSC String and a standard string.
        /// </summary>
        /// <param name="stringOne"> First string, of an OSC variety. </param>
        /// <param name="stringTwo"> Second string, the usual kind. </param>
        /// <returns> An unholy union of a string and a string, contained inside an OSC String. </returns>
        public static OscString operator +(OscString stringOne, string stringTwo)
        {
            byte[] data = new byte[stringOne.Length + stringTwo.Length];

            stringOne.CopyBytesToArray(data, 0);

            OscSerializer.AddBytes(stringTwo, data, stringOne.Length);

            return new OscString(data,
                stringOne._containsSpecialSymbols,
                stringOne._containsPatternMatching
                );

        }


        /// <summary>
        /// Allows to concatenate a standard string with an OSC String.
        /// </summary>
        /// <param name="stringOne"> First string, plain and simple. </param>
        /// <param name="stringTwo"> Second string, very OSC in its features. </param>
        /// <returns> An immense OSC String that is a combination of two strings. </returns>
        public static OscString operator +(string stringOne, OscString stringTwo)
        {
            byte[] data = new byte[stringOne.Length + stringTwo.Length];

            OscSerializer.AddBytes(stringOne, data, 0);

            stringTwo.CopyBytesToArray(data, stringOne.Length);

            return new OscString(data,
                stringTwo._containsSpecialSymbols,
                stringTwo._containsPatternMatching
                );

        }


        /// <summary>
        /// Allows to concatenate an OSC String and a char.
        /// </summary>
        /// <param name="stringOne"> First string, OSC. </param>
        /// <param name="charTwo"> Second char. </param>
        /// <returns></returns>
        public static OscString operator +(OscString stringOne, char charTwo)
        {
            byte[] data = new byte[stringOne.Length + 1];

            stringOne.CopyBytesToArray(data, 0);

            data[stringOne.Length] = Convert.ToByte(charTwo);

            return new OscString(data, stringOne._containsSpecialSymbols, stringOne._containsPatternMatching);

        }


        /// <summary>
        /// Allows to concatenate a сhar and an OSC String.
        /// </summary>
        /// <param name="charOne"> The first char. </param>
        /// <param name="stringTwo"> The second string. </param>
        public static OscString operator +(char charOne, OscString stringTwo)
        {
            byte[] data = new byte[stringTwo.Length + 1];

            stringTwo.CopyBytesToArray(data, 1);

            data[0] = Convert.ToByte(charOne);

            return new OscString(data, stringTwo._containsSpecialSymbols, stringTwo._containsPatternMatching);

        }


        /// <summary>
        /// Allows to compare OSC Strings to each other. 
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
        /// Allows to implicitly convert strings to OSC Strings.
        /// </summary>
        /// <param name="addressString"> The string to be converted. </param>
        public static implicit operator OscString(string addressString)
        {
            return new OscString(addressString);
        }


        /// <summary>
        /// Allows to explicitly convert OSC Strings to plain strings.
        /// </summary>
        /// <param name="oscString"> The OSC String to be converted. </param>
        public static explicit operator string(OscString oscString)
        {
            return oscString.ToString();
        }

        #endregion // OVERRIDES AND OPERATORS


        /// <summary>
        /// Returns true if the provided OSC String is null (only contains null symbols) or empty (doesn't contain any symbols).
        /// </summary>
        /// <param name="checkMe"> The string to check. </param>
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


        #region PATTERN-MATCHING EXTRAS
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

        #endregion // PATTERN-MATCHING EXTRAS

    }

}