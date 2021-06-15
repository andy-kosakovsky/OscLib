using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Threading;

namespace OscLib
{ 
    // TODO: Make it work with method events

    /// <summary>
    /// Implements an instance of OSC Address Space, associating C# method delegates with OSC Methods. Processes messages and bundles coming from the attached OSC Receivers, 
    /// pattern matching if needed. Can receive messages and bundles from multiple OSC Receivers.
    /// </summary>
    public class OscAddressSpace
    {
        /// <summary> The default name of the root container. </summary>
        public const string RootContainerName = "root";

        /// <summary> The name for this Address Space. </summary>
        protected string _name;

        /// <summary> The root container, from which the rest of this Address Space stems. </summary>
        protected OscContainer _root;

        /// <summary> Controls access to the elements of this Address Space when adding/removing addresses. </summary>
        protected Mutex _addressSpaceAccess;

        /// <summary> Contains all OSC Receivers this Address Space is connected to. </summary>
        protected List<OscReceiver> _receivers;

        /// <summary> Controls access to the list of connected OSC Receivers. </summary>
        protected Mutex _receiversAccess;

        /// <summary> The root container, from which the rest of this Address Space stems. </summary>
        public OscContainer Root { get => _root; }

        /// <summary>
        /// Creates a new OSC Address Space.
        /// </summary>
        public OscAddressSpace()
        {
            _root = new OscContainer(RootContainerName);
            _receivers = new List<OscReceiver>();

            _addressSpaceAccess = new Mutex();
            _receiversAccess = new Mutex();
        }

        #region CONNECTIONS

        /// <summary>
        /// Connects this address space to an OSC Receiver.
        /// </summary>
        /// <param name="receiver"> An OSC Receiver to receive messages/bundles from. </param>
        /// <exception cref="ArgumentNullException"> Thrown if the provided OSC Receiver is null. </exception>
        public void Connect(OscReceiver receiver)
        {
            if (receiver == null)
            {
                throw new ArgumentNullException(nameof(receiver));
            }

            try
            {
                _receiversAccess.WaitOne();

                if (!_receivers.Contains(receiver))
                {
                    _receivers.Add(receiver);
                    receiver.MessageReceived += ReceiveMessage;
                    receiver.BundleReceived += ReceiveBundle;
                }
                
            }
            finally
            {
                _receiversAccess.ReleaseMutex();
            }

        }


        /// <summary>
        /// Disconnects an OSC Receiver from this Address Space. Provided it was connected to begin with - otherwise nothing will happen. 
        /// </summary>
        /// <param name="receiver"> The OSC Receiver to be disconnected. </param>
        /// <exception cref="ArgumentNullException"> Thrown if the provided OSC Receiver is null. </exception>
        public void Disconnect(OscReceiver receiver)
        {
            if (receiver == null)
            {
                throw new ArgumentNullException(nameof(receiver));
            }

            try
            {
                _receiversAccess.WaitOne();

                if (_receivers.Contains(receiver))
                {
                    _receivers.Remove(receiver);
                    receiver.BundleReceived -= ReceiveBundle;
                    receiver.MessageReceived -= ReceiveMessage;
                }

            }
            finally
            {
                _receiversAccess.ReleaseMutex();
            }

        }

        #endregion // CONNECTIONS


        #region RECEIVING AND PROCESSING

        /// <summary>
        /// Processes an incoming bundle. Invoked when the connected OSC Receiver receives bundles.
        /// </summary>
        /// <param name="bundle"> OSC Bundle to process. </param>
        /// <param name="receivedFrom"> The IP end point from which the bundle was received. </param>
        public void ReceiveBundle(OscBundle bundle, IPEndPoint receivedFrom)
        {
            try
            {
                _addressSpaceAccess.WaitOne();

                if (bundle.Bundles.Length > 0)
                {
                    for (int i = 0; i < bundle.Bundles.Length; i++)
                    {
                        Process(bundle.Bundles[i].Messages);
                    }
                }

                if (bundle.Messages.Length > 0)
                {
                    Process(bundle.Messages);
                }

            }
            finally
            {
                _addressSpaceAccess.ReleaseMutex();
            }

        }


        /// <summary>
        /// Processes an incoming message. Invoked when the connected OSC Link receives a message.
        /// </summary>
        /// <param name="message"> An OSC message to process. </param>
        /// <param name="receivedFrom"> The IP end point from which the message was received. </param>
        public void ReceiveMessage(OscMessage message, IPEndPoint receivedFrom)
        {
            try
            {
                _addressSpaceAccess.WaitOne();
                Process(message);
            }
            finally
            {
                _addressSpaceAccess.ReleaseMutex();
            }
        }


        /// <summary>
        /// Processes batches of messages, invoking the appropriate OSC Methods.
        /// </summary>
        /// <param name="messages"> A batch of OSC messages to process. </param>
        protected void Process(OscMessage[] messages)
        {
            // get pattern elements

            for (int i = 0; i < messages.Length; i++)
            {
                Process(messages[i]);
            }

        }


        /// <summary>
        /// Processes a single incoming message, invoking the appropriate OSC Methods.
        /// </summary>
        /// <param name="message"> An OSC message to process. </param>
        protected void Process(OscMessage message)
        {
            OscString[] elementNames = message.AddressPattern.Split(OscProtocol.Separator);

            // internal method
            bool InvokeMethod(OscAddressElement element)
            {
                if (element is OscMethod method)
                {
                    method?.Dispatch(elementNames[elementNames.Length - 1], message.GetArguments());
                    return true;
                }

                return false;
            }

            SearchAndPerform(elementNames, false, InvokeMethod);             
        }
        #endregion // RECEIVING AND PROCESSING


        #region ELEMENT MANAGEMENT

        /// <summary>
        /// Adds a handler method to this address space. Will do one of the two things: 1. if an OSC Method already exists at the specified 
        /// address, the handler method will be connected to it; 2. otherwise, a new OSC Method will be created and added to the address
        /// space, and the handler method will be connected to it. If any elements of the address don't exist in this address space, 
        /// they will be created and added to this address space too.
        /// </summary>
        /// <param name="address"> The address to which the method should be added. Shouldn't contain any pattern-matching or reserved symbols except for separators. </param>
        /// <param name="method"> The handler method that will be attached to the OSC Method, to be invoked when an appropriate OSC Message is dispatched to this address space. </param>
        /// <returns> 
        /// The OSC Method to which the handler method was added, or null if there is already an OSC Container present at the address and 
        /// it was not possible to add the handler method.
        /// </returns>
        /// <exception cref="ArgumentException"> Thrown when the address pattern is invalid. </exception>
        public OscMethod AddMethod(OscString address, OscMethodHandler method)
        {
            OscMethod added = null;

            if (address.Length < 1)
            {
                throw new ArgumentException("OSC Address Space ERROR: Cannot add method, address pattern is invalid.");
            }

            if (method == null)
            {
                throw new ArgumentNullException(nameof(method));
            }

            // get the address pattern and check it for any crap we don't need
            OscString[] pattern = address.Split(OscProtocol.Separator);

            for (int i = 0; i < pattern.Length; i++)
            {
                if (pattern[i].ContainsPatternMatching() || pattern[i].ContainsSpecialChars())
                {
                    throw new ArgumentException("OSC Address Space ERROR: Cannot add method, address pattern contains invalid symbols.");
                }

            }

            try
            {
                _addressSpaceAccess.WaitOne();               

                OscContainer container = _root;

                for (int i = 0; i < pattern.Length; i++)
                {                 
                    // if we're not at the last bit of the address, let's find an appropriate container, or create a new one 
                    if (i != pattern.Length - 1)
                    {
                        if (container[pattern[i]] is OscContainer newContainer)
                        {
                            container = newContainer;
                        }
                        else
                        {
                            newContainer = new OscContainer(pattern[i]);
                            container.AddElement(newContainer);
                            container = newContainer;
                        }
                    }
                    else
                    {
                        if (!container.ContainsElement(pattern[i]))
                        {
                            added = new OscMethod(pattern[i]);
                            added.OscMethodInvoked += method;

                            container.AddElement(added);                           
                        }
                        else
                        {
                            if (container[pattern[i]] is OscMethod target)
                            {
                                added = target;
                                added.OscMethodInvoked += method;
                            }

                        }

                    }

                }

            }
            finally
            {
                _addressSpaceAccess.ReleaseMutex();
            }

            return added;

        }


        /// <summary>
        /// Creates an OSC Container and adds it to this address space, at the specifield address. 
        /// If any elements of the address don't exist in this address space, they will be created and added to it too.
        /// </summary>
        /// <param name="address"> The address of this OSC Container. Shouldn't contain any reserved symbols except for separators. </param>
        /// <returns> 
        /// The added container, or the existing container at the specified address (if one exists already), 
        /// or null if there is already an OSC Method at the specified address instead. 
        /// </returns>
        /// <exception cref="ArgumentException"> Thrown when the address pattern is invalid. </exception>
        public OscContainer AddContainer(OscString address)
        {
            OscContainer added = null;

            if (address.Length < 1)
            {
                throw new ArgumentException("OSC Receiver ERROR: Can't add container, address pattern is invalid.");
            }

            // get the address pattern and check it for any crap we don't need
            OscString[] pattern = address.Split(OscProtocol.Separator);

            for (int i = 0; i < pattern.Length; i++)
            {
                if (pattern[i].ContainsPatternMatching() || pattern[i].ContainsSpecialChars())
                {
                    throw new ArgumentException("OSC Address Space ERROR: Cannot add container, address pattern contains invalid symbols.");
                }

            }

            try
            {
                _addressSpaceAccess.WaitOne();

                OscContainer container = _root;

                for (int i = 0; i < pattern.Length; i++)
                {             
                    // if we're not at the last bit of the address, let's find an appropriate container, or create a new one 
                    if (i != pattern.Length - 1)
                    {
                        if (container[pattern[i]] is OscContainer newContainer)
                        {
                            container = newContainer;
                        }
                        else
                        {
                            newContainer = new OscContainer(pattern[i]);
                            container.AddElement(newContainer);
                            container = newContainer;
                        }
                    }
                    else
                    {
                        if (!container.ContainsElement(pattern[i]))
                        {
                            added = new OscContainer(pattern[i]);

                            container.AddElement(new OscContainer(pattern[i]));
                        }
                        else
                        {
                            if (container[pattern[i]] is OscContainer target)
                            {
                                added = target;
                            }
                            
                        }

                    }

                }

            }
            finally
            {
                _addressSpaceAccess.ReleaseMutex();
            }

            return added;

        }


        /// <summary>
        /// Looks for an element that corresponds to the provided address and retrieves it.
        /// If the address is a pattern, the first element that's matched to it will be retrieved.
        /// </summary>
        /// <param name="address"> The full address of the element. </param>
        /// <returns> An OSC Address Element corresponding to the provided address pattern, or null if nothing's been found. </returns>
        public OscAddressElement GetElement(OscString address)
        {
            OscAddressElement returnElement = null;

            // local function
            bool GetItOut(OscAddressElement element)
            {
                returnElement = element;
                return true;
            }

            OscString[] elementNames = address.Split(OscProtocol.Separator);

            try
            {
                _addressSpaceAccess.WaitOne();
                SearchAndPerform(elementNames, true, GetItOut);
            }
            finally
            {
                _addressSpaceAccess.ReleaseMutex();
            }

            return returnElement;
        }


        /// <summary>
        /// Looks for an element with the specified name, retrieves the first one that matches.
        /// </summary>
        /// <param name="name"> The address element's name. </param>
        /// <param name="fullAddress"> Returns the full address of the matching element. </param>
        /// <returns> The OSC Address Element with a matching name. </returns>
        public OscAddressElement GetElementByName(OscString name, out OscString fullAddress)
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// Looks for elements that match the provided pattern, returns them as a list.
        /// </summary>
        /// <param name="pattern"></param>
        /// <returns></returns>
        public List<OscAddressElement> GetElements(OscString pattern)
        {
            List<OscAddressElement> returnList = new List<OscAddressElement>();

            bool GetItOut(OscAddressElement element)
            {
                returnList.Add(element);
                return true;
            }

            OscString[] elementNames = pattern.Split(OscProtocol.Separator);

            try
            {
                _addressSpaceAccess.WaitOne();
                SearchAndPerform(elementNames, false, GetItOut);
            }
            finally
            {
                _addressSpaceAccess.ReleaseMutex();
            }


            return returnList;

        }


        /// <summary>
        /// Removes the specified address (be it a container or a method) from the address space.
        /// <para> Warning: pattern matching not yet implemented, attempts will cause an exception. </para>
        /// </summary>
        /// <param name="addressPattern"></param>
        public void RemoveElement(OscString addressPattern)
        {
            if (addressPattern.Length < 1)
            {
                throw new ArgumentException("OSC Receiver ERROR: Can't remove address, address pattern is invalid.");
            }

            try
            {
                _addressSpaceAccess.WaitOne();

                // get the address pattern and check it for any crap we don't need
                OscString[] pattern = addressPattern.Split(OscProtocol.Separator);

                OscContainer container = _root;

                for (int i = 0; i < pattern.Length; i++)
                {
                    // bumping into a reserved symbol at this stage means that there was an attempt at pattern matching, and we can't have that just yet
                    if (pattern[i].ContainsPatternMatching())
                    {
                        throw new ArgumentException("Please no pattern-matching, I beg you.");
                    }
                    
                    // if we're not at the last bit of the address, let's find out whether the next one is here 
                    if (i != pattern.Length - 1)
                    {
                        if (container[pattern[i]] is OscContainer newContainer)
                        {
                            container = newContainer;
                        }
                        else
                        {
                            throw new ArgumentException("OSC Receiver ERROR: Can't delete address " + addressPattern.ToString() + ", container " + container.Name.ToString() + " doesn't contain a container named " + pattern[i].ToString());
                        }

                    }
                    else
                    {
                        if (!container.ContainsElement(pattern[i]))
                        {
                            throw new ArgumentException("OSC Receiver ERROR: Can't delete address " + addressPattern.ToString() + ", container " + container.Name.ToString() + " doesn't contain an address " + pattern[i].ToString());
                        }
                        else
                        {
                            // delete the part
                            container.RemoveElement(pattern[i]);                           
                        }

                    }

                }

            }
            finally
            {
                _addressSpaceAccess.ReleaseMutex();
            }

        }
        #endregion // ELEMENT MANAGEMENT


        /// <summary>
        /// Searches the Address Space and performs the provided action on the elements adhering to the provided pattern, taken as an array of element names.
        /// </summary>
        /// <param name="elementNames"> An array of OSC Address Element names that make up an address pattern. </param>
        /// <param name="performOnlyOnce"> Whether the function will only be performed on the first eligible element. </param>
        /// <param name="function"> The function to perform: 
        /// <para> -- takes one parameter (the Address Element that matches the pattern); </para>
        /// <para> -- returns a boolean (whether the function could be performed on the provided Address Elementwas successful). </para> </param>
        protected void SearchAndPerform(OscString[] elementNames, bool performOnlyOnce, Func<OscAddressElement, bool> function)
        {
            // the layer of the pattern that contains the important name
            int activeLayer = elementNames.Length - 1;

            int currentLayer = 0;

            // array for the container stack. its length equals the maximum depth at which we'll be checking
            OscContainer[] stack = new OscContainer[elementNames.Length];

            // array for layer indices - i. e. what was the last index we checked when we were at this layer. if the index is -1, go up a layer
            int[] indices = new int[elementNames.Length];

            // add root to stack
            stack[currentLayer] = _root;

            // horrible, horrible iterative bit to avoid recursion (these address spaces might get just a tad too large for safe use of recursion)
            while (currentLayer >= 0)
            {
                // index of -1 for the current layer indicates that we're done with this particular layer and can safely go back
                if (indices[currentLayer] == -1)
                {
                    currentLayer--;
                    continue;
                }

                // if we're at method layer of the received message, let's check for methods, otherwise we'll be checking for containers
                if (currentLayer == activeLayer)
                {
                    // if we don't have any reserved symbols, that means there should be only one method adhering to the pattern
                    if (!elementNames[currentLayer].ContainsPatternMatching())
                    {
                        // perform the Function(tm) on that single element, break if needed and if successful
                        if (function(stack[currentLayer][elementNames[currentLayer]]))
                        {
                            if (performOnlyOnce)
                            {
                                break;
                            }

                        }

                        // go up a layer
                        indices[currentLayer] = -1;
                        currentLayer--;

                    }
                    else
                    {
                        for (int j = 0; j < stack[currentLayer].Length; j++)
                        {
                            // perform the Action(tm) if the element is a method and adheres to the pattern
                            if (stack[currentLayer][j].Name.PatternMatch(elementNames[currentLayer]))
                            {
                                if (function(stack[currentLayer][j]))
                                {
                                    if (performOnlyOnce)
                                    {
                                        break;
                                    }

                                }

                            }
                            
                        }
                        // go up a layer
                        indices[currentLayer] = -1;
                        currentLayer--;

                    }

                }
                else // we don't expect a method at the current layer, but we still need to check the containers
                {
                    if (!elementNames[currentLayer].ContainsPatternMatching())
                    {
                        if (stack[currentLayer][elementNames[currentLayer]] is OscContainer container)
                        {
                            // it's the only container we need, so index for the current layer should be set to -1
                            indices[currentLayer] = -1;

                            // then, let's go down a layer, and prepare everything for the next cycle 
                            currentLayer++;
                            stack[currentLayer] = container;
                            indices[currentLayer] = 0;
                        }
                        else
                        {
                            // if we couldn't find the container, return up a layer
                            indices[currentLayer] = -1;
                            currentLayer--;

                        }

                    }
                    else
                    {
                        // go through address parts contained in this container, trying to pattern match
                        // the cycle starts from where it was left off the previous time we were at this level
                        for (int j = indices[currentLayer]; j < stack[currentLayer].Length; j++)
                        {
                            if (stack[currentLayer][j] is OscContainer container)
                            {
                                // shift to that layer
                                if (container.Name.PatternMatch(elementNames[currentLayer]))
                                {

                                    indices[currentLayer] = j + 1;

                                    currentLayer++;
                                    stack[currentLayer] = container;
                                    indices[currentLayer] = 0;
                                    break;
                                }

                            }

                            // save the next index for this layer 
                            indices[currentLayer] = j + 1;

                        }

                        // if we've reached the end of this cycle, clear this layer and go back one layer
                        if (indices[currentLayer] >= stack[currentLayer].Length)
                        {
                            indices[currentLayer] = -1;
                            currentLayer--;
                        }

                    }

                }

            }

        }

        public override string ToString()
        {
            StringBuilder returnString = new StringBuilder("OSC Address Space; OSC Links connected: ");

            try
            {

                _receiversAccess.WaitOne();

                for (int i = 0; i < _receivers.Count; i++)
                {
                    returnString.Append('[');
                    returnString.Append(i);
                    returnString.Append(']');
                    returnString.Append(": ");
                    returnString.Append(_receivers[i].ToString());
                    returnString.Append("; ");
                }

            }
            finally
            {
                _receiversAccess.ReleaseMutex();
            }

            return returnString.ToString();

        }


        /// <summary>
        /// Prints the entire OSC Address Space as an address tree, 
        /// </summary>
        /// <returns></returns>
        public string PrintAddressTree()
        {
            StringBuilder returnString = new StringBuilder("RECEIVER ADDRESS SPACE:\n");

            try
            {
                _addressSpaceAccess.WaitOne();
                int currentLayer = 0;

                List<OscContainer> currentPath = new List<OscContainer>();
                List<int> indices = new List<int>();

                // add root
                currentPath.Add(_root);
                indices.Add(0);

                returnString.Append("ROOT (");
                returnString.Append(_root.Length);
                returnString.Append(')');
                returnString.Append('\n');


                while (currentLayer >= 0)
                {
                    // index of -1 for the current layer indicates that we're done with this particular layer and can safely go back
                    if (indices[currentLayer] >= currentPath[currentLayer].Length)
                    {
                        returnString.Append('\n');
                        indices.RemoveAt(currentLayer);
                        currentPath.RemoveAt(currentLayer);
                        currentLayer--;
                        continue;
                    }

                    // append the spaces to designate the current depth
                    returnString.Append(OscUtil.GetRepeatingChar(' ', (currentLayer + 1) * 4));

                    if (currentPath[currentLayer][indices[currentLayer]] is OscContainer container)
                    {
                        returnString.Append(container.ToString());
                        returnString.Append('\n');

                        indices[currentLayer]++;
                        // then, let's go up a layer, and prepare everything for the next cycle 
                        currentLayer++;
                        currentPath.Add(container);
                        indices.Add(0);
                    }
                    else if (currentPath[currentLayer][indices[currentLayer]] is OscMethod method)
                    {
                        returnString.Append(method.ToString());
                        returnString.Append('\n');
                        indices[currentLayer]++;
                    }

                }

            }
            finally
            {
                _addressSpaceAccess.ReleaseMutex();
            }

            return returnString.ToString();
        }

    }

}
