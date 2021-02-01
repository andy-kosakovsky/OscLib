using System;
using System.Text;

namespace OscLib
{

    /// <summary>
    /// Contains an ASCII string recorded as an array of bytes - to avoid dealing with actual strings. Can be accessed via an indexer.
    /// </summary>
    public struct OscString
    {
        private readonly byte[] _chars;
        private readonly int _oscLength;
        private readonly int _length;
        private Trit _containsReservedSymbols;

        /// <summary> The length of this string in bytes when used as an OSC argument or address. </summary> 
        public int OscLength { get => _oscLength; }

        /// <summary> The length of this string in bytes. </summary>
        public int Length { get => _length; }

        /// <summary> Returns a copy of an array containing all chars (their byte representations, that is) that constitute this string. </summary>
        public byte[] Chars
        {
            get
            {
                byte[] copy = new byte[_length];
                _chars.CopyTo(copy, 0);
                return copy;
            }

        }

        /// <summary> Returns a copy of an array containing all chars (their byte representations, that is) that is of the right length to be used as an element in OSC binary packet. </summary>
        public byte[] OscBytes
        {
            get
            {
                byte[] copy = new byte[_oscLength];
                _chars.CopyTo(copy, 0);
                return copy;
            }

        }

        /// <summary>
        /// Whether this string contains special symbols reserved by OSC protocol. 
        /// </summary>
        public bool ContainsReservedSymbols 
        {
            get
            {
                if (_containsReservedSymbols == Trit.Maybe)
                {
                    // let's find out, shall we
                    bool contains = OscUtil.ContainsReservedSymbols(_chars);

                    if (contains)
                    {
                        _containsReservedSymbols = Trit.True;
                        return true;
                    }
                    else
                    {
                        _containsReservedSymbols = Trit.False;
                        return false;
                    }
                    
                }
                else if (_containsReservedSymbols == Trit.True)
                {
                    return true;
                }
                else
                {
                    return false;
                }

            }

        }

        /// <summary>
        /// Indexer access to get the chars, as ASCII bytes.
        /// </summary>
        /// <param name="i"> Index of the char. </param>
        /// <returns> A char in byte form. </returns>
        public byte this[int i] { get => _chars[i]; }


        /// <summary>
        /// Creates an OSC string out of a byte array.
        /// </summary>
        /// <param name="bytes"> The source byte array. </param>
        public OscString(byte[] bytes)
        {
            _chars = bytes;
            _oscLength = OscUtil.GetNextMultipleOfFour(_chars.Length);
            _length = _chars.Length;
            _containsReservedSymbols = Trit.Maybe;
        }


        /// <summary>
        /// Creates an OSC string out of a part of a byte array.
        /// </summary>
        /// <param name="bytes"> The source byte array. </param>
        /// <param name="start"> The start index. </param>
        /// <param name="length"> The length of the string. </param>
        public OscString(byte[] bytes, int start, int length)
        {
            _chars = new byte[length];
            Array.Copy(bytes, start, _chars, 0, length);

            _oscLength = OscUtil.GetNextMultipleOfFour(_chars.Length);
            _length = _chars.Length;
            _containsReservedSymbols = Trit.Maybe;
        }


        /// <summary>
        /// Creates an OSC string out of a plain old string.
        /// </summary>
        /// <param name="addressString"> The source plain old string. </param>
        public OscString(string addressString)
        {
            _chars = Encoding.ASCII.GetBytes(addressString);
            
            _oscLength = OscUtil.GetNextMultipleOfFour(_chars.Length);
            _length = _chars.Length;
            _containsReservedSymbols = Trit.Maybe;
        }


        /// <summary>
        /// Removes any symbols from the string that are reserved by the OSC protocol, swapping them with "_", and returns the resulting string
        /// </summary>
        public OscString ScrubReservedSymbols()
        {
            byte[] newStringBytes = new byte[_chars.Length];
            _chars.CopyTo(newStringBytes, 0);

            for (int i = 0; i < newStringBytes.Length; i++)
            {
                if (OscUtil.IsAReservedSymbol(newStringBytes[i]))
                {
                    newStringBytes[i] = (byte)'_';
                }
            }

            return new OscString(newStringBytes);

        }


        /// <summary>
        /// Removes any symbols from the string that are reserved by the OSC protocol, swapping them with "_", and returns the resulting string.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static OscString ScrubReservedSymbols(string input)
        {
            byte[] newStringBytes = new byte[input.Length];
            Encoding.ASCII.GetBytes(input).CopyTo(newStringBytes, 0);

            for (int i = 0; i < newStringBytes.Length; i++)
            {
                if (OscUtil.IsAReservedSymbol(newStringBytes[i]))
                {
                    newStringBytes[i] = (byte)'_';
                }
            }

            return new OscString(newStringBytes);

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
            OscString copy = new OscString(this._chars);
            copy._containsReservedSymbols = _containsReservedSymbols;

            return copy;
        }


        /// <summary>
        /// Splits the OSC string into an array of new OSC string, using the provided symbol.
        /// </summary>
        /// <param name="splitByte"> The ASCII encoding of a symbol by which to split the string. </param>
        /// <returns> An array of resulting OSC strings. </returns>
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
            if (start >= this._length)
                throw new ArgumentException("OSC String ERROR: Cannot get a substring, it starts beyond the length of the string.");

            if (start + length > _length)
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
            // first, let's cover some common situations
            // if pattern consists of only one "*" symbol then it'll match to anything
            if ((pattern._chars.Length == 1) && (pattern[0] == OscProtocol.SymbolAsterisk))
            {
                return true;
            }

            // if strings are the same, might as well return true
            if (!pattern.ContainsReservedSymbols)
            {
                return (this == pattern);
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
                if (pattern[patIndex] == OscProtocol.SymbolAsterisk)
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
                else if (pattern[patIndex] == OscProtocol.SymbolOpenSquare)
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
                else if (pattern[patIndex] == OscProtocol.SymbolOpenCurly)
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
                    if (strRevert >= this.Length)
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

            while ((patIndex < pattern.Length) && (pattern[patIndex] == OscProtocol.SymbolAsterisk))
            {
                patIndex++;
            }

            return (patIndex == pattern.Length);

        }

        // overrides etc.

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
                return (this == (OscString)stringString);
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

                int hash = 1;

                for (int i = 0; i < _chars.Length; i++)
                {
                    hash += _chars[i] + 1;
                }

                return hash;

            }

        }

         
        // used in pattern matching - will return true if char is compared to itself or a special symbol such as * or ?
        private bool CharIsEqual(byte strChar, byte patChar)
        {
            if (patChar == OscProtocol.SymbolQuestion)
            {
                return true;
            }
            else
            {
                return (strChar == patChar);
            }
               
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
                if (pattern[pointer] == OscProtocol.SymbolClosedSquare)
                {
                    bracketEnd = pointer;
                    // make sure the pointer is going past the bracket
                    pointer++;
                    break;

                }

                if (!found)
                {

                    if (pattern[pointer] == OscProtocol.SymbolExclamation)
                    {
                        // if it's at the beginning, make sure that it's noted, and keep a space for it in the return array 
                        if (bracketStart == (pointer - 1))
                        {
                            reverse = true;
                        }

                    }
                    else if (pattern[pointer] == OscProtocol.SymbolDash)
                    {
                        // if we're not at the start, and if we're not by the end of the char array, so we can safely check back and forth
                        if ((pointer > bracketStart + 1) && (((pointer + 1) < pattern.Length) && (pattern[pointer + 1] != OscProtocol.SymbolClosedSquare)))
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
                if ((pattern[patPointer] == OscProtocol.SymbolComma) || (pattern[patPointer] == OscProtocol.SymbolClosedCurly))
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
                                  
                    if (pattern[patPointer] == OscProtocol.SymbolClosedCurly)
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

    }

}
