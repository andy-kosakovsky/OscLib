using System;
using System.Collections.Generic;
using System.Text;

namespace OscLib
{
    /// <summary>
    /// Represents an OSC Address Space method container - a folder for other address elements.
    /// </summary>
    public sealed class OscContainer : OscAddressElement
    {
        /// <summary> All child elements inside this container. </summary>
        private List<OscAddressElement> _contents;

        /// <summary> Connects the names of elements with their indeces in the "contents" list, to ease lookup and pattern-matching. </summary>
        private Dictionary<OscString, int> _contentsNames;

        /// <summary> Returns the number of elements within this <see cref="OscContainer"/>. </summary>
        public int Length { get => _contents.Count; }


        /// <summary>
        /// Indexer access to elements inside this <see cref="OscContainer"/>, using their names.
        /// </summary>
        /// <param name="name"> Name of the element. </param>
        /// <returns> The <see cref="OscAddressElement"/> with the provided name - or null, if there isn't one. </returns>
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
        /// Indexer access to elements inside this <see cref="OscContainer"/>, using their indices.
        /// </summary>
        /// <param name="index"> Index of the element. </param>
        /// <returns> The <see cref="OscAddressElement"/> under the provided index - or null, if the index is out of bounds. </returns>
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
        /// Initializes a new instance of the <see cref="OscContainer"/> class, with a specified name.
        /// </summary>
        /// <param name="name"> The name of this <see cref="OscContainer"/>. Note: no need to include a "/" symbol. </param>
        internal OscContainer(OscString name) 
            :base(name)
        {
            _contentsNames = new Dictionary<OscString, int>();
            _contents = new List<OscAddressElement>();
        }


        /// <summary>
        /// Adds an <see cref="OscAddressElement"/> to this <see cref="OscContainer"/>.
        /// </summary>
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
        /// Removes an <see cref="OscAddressElement"/> from this <see cref="OscContainer"/> - provided it's present.
        /// </summary>
        /// <returns> "True" if an element was removed, "False" if nothing was found. </returns>
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
        /// Removes an <see cref="OscAddressElement"/> with the specified name from this <see cref="OscContainer"/> - provided it's present.
        /// </summary>
        /// <remarks> Doesn't perform pattern-matching. </remarks>
        /// <returns> "True" if an element was removed, "False" if nothing was found. </returns>
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
        /// Removes every <see cref="OscAddressElement"/> with a name that matches the provided pattern from this <see cref="OscContainer"/>.
        /// </summary>
        /// <remarks> Does perform pattern-matching. </remarks>
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
        /// Checks if this <see cref="OscContainer"/> contains the specified <see cref="OscAddressElement"/>.
        /// </summary>
        public bool ContainsElement(OscAddressElement element)
        {
            return _contents.Contains(element);
        }


        /// <summary>
        /// Checks if this <see cref="OscContainer"/> contains an <see cref="OscAddressElement"/> with the provided name, or with a name matching the provided pattern.
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
        /// Returns an array of every <see cref="OscAddressElement"/> in this <see cref="OscContainer"/> with a name that matches the specified pattern.
        /// </summary>
        public List<OscAddressElement> GetElements(OscString pattern)
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
        /// Retrieves an <see cref="OscAddressElement"/> with the specified name from this <see cref="OscContainer"/> - provided it's present.
        /// </summary>
        /// <remarks> Pattern-matching won't work with this method - it will likely return a null. </remarks>
        /// <param name="name"> The name of the element. </param>
        /// <returns> The <see cref="OscAddressElement"/> with the specified name, or null if nothing was found. </returns>
        public OscAddressElement GetElement(OscString name)
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
        /// Returns the index of an <see cref="OscAddressElement"/> with the specified name within this <see cref="OscContainer"/> - provided it's present.
        /// </summary>
        /// <remarks> Pattern-matching won't work with this method - it will likely return a negative. </remarks>
        /// <param name="name"> The name of the element. </param>
        /// <returns> The index of the <see cref="OscAddressElement"/> with the specified name in this container, or -1 if nothing was found. </returns>
        public int GetElementIndex(OscString name)
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
        /// Returns the index of the specified <see cref="OscAddressElement"/>, if it's contained in this <see cref="OscContainer"/>.
        /// </summary>
        /// <remarks> This method will specificly look for the provided element - other elements with the same name don't count. </remarks>
        /// <returns> The index of the element in this container, or -1 if nothing was found. </returns>
        public int GetElementIndex(OscAddressElement element)
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
        private void RefreshNames()
        {
            _contentsNames.Clear();

            for (int i = 0; i < _contents.Count; i++)
            {
                _contentsNames.Add(_contents[i].Name, i);
            }

        }


        /// <summary>
        /// Returns a string containing the name of this <see cref="OscContainer"/> and the total number of <see cref="OscAddressElement"/> instances inside.
        /// </summary>
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
