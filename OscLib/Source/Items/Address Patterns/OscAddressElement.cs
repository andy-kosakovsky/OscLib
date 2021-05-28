using System;
using System.Collections.Generic;
using System.Text;

namespace OscLib
{
    /// <summary>
    /// A base class for representing elements of OSC Address Spaces
    /// </summary>
    public abstract class OscAddressElement
    {
        /// <summary>
        /// Name of this element.
        /// </summary>
        protected readonly OscString _name;

        /// <summary>
        /// Name of this element. 
        /// </summary>
        public OscString Name { get => _name; }

        /// <summary>
        /// Creates a new address element.
        /// </summary>
        /// <param name="name"> Name of this element. Can't contain reserved symbols or symbols involved in pattern-matching - no need to start it with a "/". </param>
        /// <exception cref="ArgumentException"> Thrown when name does contain reserved symbols. Pls no, thx. </exception>
        public OscAddressElement(OscString name)
        {
            if (name.ContainsSpecialSymbols() || name.ContainsPatternMatching())
            {
                throw new ArgumentException("Address Space Error: Invalid name for new element, contains reserved symbols");
            }

            _name = name;
        }

        /// <summary>
        /// Returns the name of this address part as a standard string.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return _name.ToString();
        }


    }
}
