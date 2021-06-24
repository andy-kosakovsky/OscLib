using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace OscLib
{
    // TODO: Add a "OscAdressElement[] GetElements(pattern)" method that would return all elements within this container that match to a pattern

    /// <summary>
    /// Represents an OSC Address Space method container - a folder for other address elements.
    /// </summary>
    public class OscContainer : OscAddressElement
    {
        /// <summary> All child elements inside this container. </summary>
        protected List<OscAddressElement> _contents;

        /// <summary> Connects the names of elements with their indeces in the "contents" list, to ease lookup and pattern-matching. </summary>
        protected Dictionary<OscString, int> _contentsNames;

        /// <summary> The number of elements within this container. </summary>
        public int Length { get => _contents.Count; }


        /// <summary>
        /// Indexer access to the elements of this container, using their names.
        /// </summary>
        /// <param name="name"> Name of the element. </param>
        /// <returns> The element with the provided name - or null, if there isn't one. </returns>
        public OscAddressElement this[OscString name] 
        { 
            get
            {
                if (_contentsNames.ContainsKey(name))
                {
                    return _contents[_contentsNames[name]];
                }
                else
                {
                    return null;
                }

            }
        
        }


        /// <summary>
        /// Indexer access to the elements of this container, using their indices.
        /// </summary>
        /// <param name="index"> Index of the element. </param>
        /// <returns> The element under the provided index - or null, if the index is out of bounds. </returns>
        public OscAddressElement this[int index]
        {
            get
            {
                if (index.IsNumberBetween(0, _contents.Count - 1))
                {
                    return _contents[index];
                }
                else
                {
                    return null;
                }

            }

        }


        /// <summary>
        /// Creates a new OSC Container.
        /// </summary>
        /// <param name="name"> The name for this container. Note: no need to include a "/" symbol. </param>
        internal OscContainer(OscString name) 
            :base(name)
        {
            _contentsNames = new Dictionary<OscString, int>();
            _contents = new List<OscAddressElement>();
        }


        /// <summary>
        /// Adds an address element to this container.
        /// </summary>
        /// <param name="element"> The element to add. </param>
        /// <exception cref="ArgumentException"> Thrown when this container already contains an element with this name. </exception>
        public void AddElement(OscAddressElement element)
        {
            if (_contentsNames.ContainsKey(element.Name))
            {
                throw new ArgumentException("OSC Address ERROR: Can't add element " + element.ToString() + " to OSC Container " + _name.ToString() + "; " + _name.ToString() + " already contains an element with that name. ");
            }

            _contents.Add(element);
            RefreshNames();

            element.ChangeParent(this);

        }


        /// <summary>
        /// Removes an address element from this container - if it's inside this container.
        /// </summary>
        /// <returns> True if an element was removed, false if nothing was found. </returns>
        public bool RemoveElement(OscAddressElement element)
        {
            if (_contents.Contains(element))
            {
                _contents.Remove(element);
                RefreshNames();

                element.ChangeParent(null);

                return true;
            }

            return false;

        }


        /// <summary>
        /// Removes an address element with the specified name from this container, if it's inside this container.
        /// </summary>
        /// <returns> True if an element was removed, false if nothing was found. </returns>
        public bool RemoveElement(OscString elementName)
        {
            if (_contentsNames.ContainsKey(elementName))
            {
                _contents[_contentsNames[elementName]].ChangeParent(null);

                _contents.RemoveAt(_contentsNames[elementName]);

                RefreshNames();

                return true;
            }

            return false;

        }


        /// <summary>
        /// Removes all address elements whose names adhere to the provided pattern from this container.
        /// </summary>
        /// <returns> The total number of removed elements. </returns>
        public int RemoveElements(OscString pattern)
        {
            int removed = 0;

            for (int i = _contents.Count - 1; i >= 0; i--)
            {
                if (_contents[i].Name.PatternMatch(pattern))
                {
                    _contents[i].ChangeParent(null);

                    _contents.RemoveAt(i);
                    removed++;
                }

            }

            RefreshNames();
            return removed;

        }


        /// <summary>
        /// Checks if this Container contains the specified address element.
        /// </summary>
        public bool ContainsElement(OscAddressElement element)
        {
            return _contents.Contains(element);
        }


        /// <summary>
        /// Checks if this Container contains an OSC Address Element with the provided name (or matching the provided pattern).
        /// </summary>
        public bool ContainsElement(OscString pattern)
        {           
            // if it's a pattern
            if (pattern.ContainsPatternMatching())
            {
                for (int i = 0; i < _contents.Count; i++)
                {
                    if (_contents[i].Name.PatternMatch(pattern))
                    {
                        return true;
                    }

                }

                return false;

            }
            else
            {
                return _contentsNames.ContainsKey(pattern);
            }

        }


        /// <summary>
        /// Returns an array of elements in this container that match the specified pattern.
        /// </summary>
        public virtual List<OscAddressElement> GetElements(OscString pattern)
        {
            List<OscAddressElement> list = new List<OscAddressElement>(_contents.Count);

            for (int i = 0; i < _contents.Count; i++)
            {
                if (_contents[i].Name.PatternMatch(pattern))
                {
                    list.Add(_contents[i]);
                }

            }

            return list;

        }

        /// <summary>
        /// Returns an element with the specified name.
        /// </summary>
        /// <remarks> Pattern-matching won't work with this method - it will likely return a null. </remarks>
        /// <param name="name"> The name of the element. </param>
        /// <returns> The element with the specified name, or null if nothing was found. </returns>
        public virtual OscAddressElement GetElement(OscString name)
        {
            if (_contentsNames.ContainsKey(name))
            {
                return _contents[_contentsNames[name]];
            }
            else
            {
                return null;
            }

        }


        /// <summary>
        /// Returns the index of an element with the specified name.
        /// </summary>
        /// <remarks> Pattern-matching won't work with this method - it will likely return a negative. </remarks>
        /// <param name="name"> The name of the element. </param>
        /// <returns> The index of the element with the specified name in this container, or -1 if nothing was found. </returns>
        public virtual int GetElementIndex(OscString name)
        {
            if (_contentsNames.ContainsKey(name))
            {
                return _contentsNames[name];
            }
            else
            {
                return -1;
            }

        }


        /// <summary>
        /// Returns the index of the specified element, if it's contained in this container.
        /// </summary>
        /// <remarks> This method will specificly look for the provided element - other elements with the same name don't count. </remarks>
        /// <returns> The index of the element in this container, or -1 if nothing was found. </returns>
        public virtual int GetElementIndex(OscAddressElement element)
        {
            if (_contents.Contains(element))
            {
                return _contentsNames[element.Name];
            }
            else
            {
                return -1;
            }

        }


        /// <summary>
        /// Used to update the element names and the corresponding indices in the name dictionary.
        /// </summary>
        protected void RefreshNames()
        {
            _contentsNames.Clear();

            for (int i = 0; i < _contents.Count; i++)
            {
                _contentsNames.Add(_contents[i].Name, i);
            }

        }

        public override string ToString()
        {
            StringBuilder returnString = new StringBuilder();
            returnString.Append("CONTAINER: ");
            returnString.Append(_name.ToString());
            returnString.Append(" (elements inside: ");
            returnString.Append(Length);
            returnString.Append(')');

            return returnString.ToString();

        }

    }

}
