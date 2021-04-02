using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace OscLib
{
    /// <summary>
    /// Represents an OSC method container, to be used within the OSC Address Space - basically a folder for methods and containers.
    /// </summary>
    public class OscContainer : OscAddressPart
    {
        private List<OscAddressPart> _contents;
        // this dictionary links the indices of elements inside the list with their names, for ease of access and checks.
        private Dictionary<OscString, int> _contentsNames;

        /// <summary> The number of elements within this container. </summary>
        public int Length { get => _contents.Count; }

        /// <summary>
        /// Indexer access to the elements of this container, using their names.
        /// </summary>
        /// <param name="name"> Name of the element. </param>
        /// <returns> The element with the provided name, or null if it's not in this container. </returns>
        public OscAddressPart this[OscString name] 
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
        /// <returns> The element under the provided index. </returns>
        public OscAddressPart this[int index]
        {
            get
            {
                return _contents[index];
            }

        }

        /// <summary>
        /// Creates a new OSC Container.
        /// </summary>
        /// <param name="name"></param>
        public OscContainer(OscString name) 
            :base(name)
        {
            _contentsNames = new Dictionary<OscString, int>();
            _contents = new List<OscAddressPart>();
        }

        /// <summary>
        /// Adds an OSC container or a method to this container.
        /// </summary>
        /// <param name="part"></param>
        public void AddPart(OscAddressPart part)
        {                       
            _contents.Add(part);
            // new address part should be the last index of the list 
            RefreshNames();                
        }

        /// <summary>
        /// Removes an OSC container or a method from this container, if it's inside this container.
        /// </summary>
        /// <param name="part"></param>
        public void RemovePart(OscAddressPart part)
        {
            if (_contentsNames.ContainsKey(part.Name))
            {             
                _contents.Remove(part);
                RefreshNames();
            }

        }

        /// <summary>
        /// Removes an OSC container or a method with a specified name from this container, if it's inside this container.
        /// </summary>
        /// <param name="partName"></param>
        /// <exception cref="ArgumentException"> Thrown when an element with the provided name is not present inside this container. </exception>
        public void RemovePart(OscString partName)
        {
            if (!_contentsNames.ContainsKey(partName))
            {
                throw new ArgumentException("Address Pattern ERROR: OSC Container " + _name + " doesn't have an element with name " + partName);
            }    

            _contents.RemoveAt(_contentsNames[partName]);
            RefreshNames();
     
        }

        /// <summary>
        /// Checks if container contains an address part with a provided name (or adhering to a provided pattern).
        /// </summary>
        public bool ContainsPart(OscString partName)
        {           
            // if it's a pattern
            if (partName.ContainsReservedSymbols())
            {
                for (int i = 0; i < _contents.Count; i++)
                {
                    if (_contents[i].Name.PatternMatch(partName))
                    {
                        return true;
                    }

                }

                return false;

            }
            else
            {
                return _contentsNames.ContainsKey(partName);
            }

        }

        private void RefreshNames()
        {
            _contentsNames.Clear();

            for (int i = 0; i < _contents.Count; i++)
            {
                _contentsNames.Add(_contents[i].Name, i);
            }

        }

    }

}
