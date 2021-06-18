using System;
using System.Collections.Generic;
using System.Text;

namespace OscLib
{
    /// <summary>
    /// A base class for representing elements of OSC Address Spaces.
    /// </summary>
    public abstract class OscAddressElement
    {
        /// <summary> The name of this element. </summary>
        protected readonly OscString _name;

        /// <summary> The parent element of this element within the encompassing OSC Address Space. </summary>
        protected OscContainer _parent;

        /// <summary> The name of this element. </summary>
        public OscString Name { get => _name; }

        /// <summary> The parent element of this element within the encompassing OSC Address Space. </summary>
        public OscContainer Parent { get => _parent; }


        /// <summary>
        /// Creates a new address element.
        /// </summary>
        /// <param name="name"> Name of this element. Can't contain reserved symbols or symbols involved in pattern-matching - no need to start it with a "/". </param>
        /// <exception cref="ArgumentNullException"> Thrown when the provided name is null or empty. </exception>
        /// <exception cref="ArgumentException"> Thrown when the name does contain reserved symbols. Pls no, thx. </exception>
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
            _parent = null;
        }


        /// <summary>
        /// Returns the name of this address element as string.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return _name.ToString();
        }


        internal virtual void ChangeParent(OscContainer newParent)
        {
            _parent = newParent;
        }


        /// <summary>
        /// Returns the address of this element within the encompassing Address Space.
        /// </summary>
        /// <returns></returns>
        public OscString GetAddress()
        {
            List<OscString> nameList = new List<OscString>();

            OscAddressElement currentElement = this;

            while (currentElement != null)
            {
                nameList.Add('/' + currentElement.Name);
                currentElement = currentElement.Parent;
            }

            // remove last element - it'll just be the root container anyway
            nameList.RemoveAt(nameList.Count - 1);

            nameList.Reverse();

            return new OscString(nameList.ToArray());

        }

    }

}
