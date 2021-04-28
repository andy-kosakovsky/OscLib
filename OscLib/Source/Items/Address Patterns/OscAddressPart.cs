using System;
using System.Collections.Generic;
using System.Text;

namespace OscLib
{
    /// <summary>
    /// Represents a part of an OSC Address Space.
    /// </summary>
    public abstract class OscAddressPart
    {
        /// <summary>
        /// Name of this address part.
        /// </summary>
        protected readonly OscString _name;

        /// <summary>
        /// Name of this address part. 
        /// </summary>
        public OscString Name { get => _name; }

        /// <summary>
        /// Creates a new address part.
        /// </summary>
        /// <param name="name"> Name of this address part. Can't contain reserved symbols. </param>
        /// <exception cref="ArgumentException"> Thrown when name does contain reserved symbols. Pls no, thx. </exception>
        public OscAddressPart(OscString name)
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
