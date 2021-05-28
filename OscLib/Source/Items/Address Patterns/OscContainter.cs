using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace OscLib
{
    // TODO: Add a "OscAdressElement[] GetElements(pattern)" method that would return all elements within this container that match to a pattern

    /// <summary>
    /// Represents an OSC method container, to be used within the OSC Address Space - basically a folder for other address elements.
    /// </summary>
    public class OscContainer : OscAddressElement
    {
        /// <summary> All child elements contained in this container. </summary>
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
                if (OscUtil.IsNumberBetween(index, 0, _contents.Count - 1))
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
        public OscContainer(OscString name) 
            :base(name)
        {
            _contentsNames = new Dictionary<OscString, int>();
            _contents = new List<OscAddressElement>();
        }

        /// <summary>
        /// Adds an address element to this container.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="part"> The element to add. </param>
        /// <exception cref="ArgumentException"> Thrown when this container already contains an element with this name. </exception>
        public void AddElement(OscAddressElement element)
        {
            if (_contentsNames.ContainsKey(element.Name))
            {
                throw new ArgumentException("OSC Address ERROR: Can't add element " + element.ToString() + " to OSC Container " + this.ToString() + "; " + this.ToString() + " already contains an element with that name. ");
            }

            _contents.Add(element);
            // new address part should be the last index of the list 
            RefreshNames();                
        }

        /// <summary>
        /// Removes an address element from this container - if it's inside this container.
        /// </summary>
        /// <param name="element"></param>
        public void RemoveElement(OscAddressElement element)
        {
            if (_contents.Contains(element))
            {
                _contents.Remove(element);
                RefreshNames();
            }

        }

        /// <summary>
        /// Removes an address element with the specified name from this container, if it's inside this container.
        /// </summary>
        /// <param name="elementName"></param>
        public void RemoveElement(OscString elementName)
        {
            if (_contentsNames.ContainsKey(elementName))
            {
                _contents.RemoveAt(_contentsNames[elementName]);
                RefreshNames();
            }    

        }

        /// <summary>
        /// Checks if container contains an address element with the provided name (or adhering to the provided pattern).
        /// </summary>
        public bool ContainsElement(OscString elementName)
        {           
            // if it's a pattern
            if (elementName.ContainsPatternMatching())
            {
                for (int i = 0; i < _contents.Count; i++)
                {
                    if (_contents[i].Name.PatternMatch(elementName))
                    {
                        return true;
                    }

                }

                return false;

            }
            else
            {
                return _contentsNames.ContainsKey(elementName);
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

    }

}
