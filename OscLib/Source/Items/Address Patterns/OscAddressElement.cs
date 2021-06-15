using System;
using System.Collections.Generic;
using System.Text;

namespace OscLib
{
    /// <summary>
    /// A base class for representing elements of OSC Address Spaces.
    /// <remarks>
    /// The idea is that this class and its derivatives would only be used in conjunction with the OscAddressSpace class. 
    /// </remarks>
    /// </summary>
    public abstract class OscAddressElement
    {
        /// <summary>
        /// The name of this element.
        /// </summary>
        protected readonly OscString _name;

        /// <summary>
        /// The address of this element within the encompassing OSC Address Space.
        /// </summary>
        protected OscString _address;

        /// <summary>
        /// The name of this element. 
        /// </summary>
        public OscString Name { get => _name; }

        /// <summary>
        /// The address of this element within the encompassing OSC Address Space.
        /// </summary>
        public OscString Address { get => _address; }


        /// <summary>
        /// Creates a new address element.
        /// </summary>
        /// <param name="name"> Name of this element. Can't contain reserved symbols or symbols involved in pattern-matching - no need to start it with a "/". </param>
        /// <exception cref="ArgumentException"> Thrown when name does contain reserved symbols. Pls no, thx. </exception>
        internal OscAddressElement(OscString name)
        {
            if (OscString.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (name.ContainsSpecialChars() || name.ContainsPatternMatching())
            {
                throw new ArgumentException("Address Space Error: Invalid name for new element, contains reserved symbols");
            }

            _name = name;
            _address = OscString.NullString;
        }


        /// <summary>
        /// Returns the name of this address element as string.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return _name.ToString();
        }


        internal virtual void ChangeAddress(OscString newAddress)
        {
            _address = newAddress;
        }

    }

}
