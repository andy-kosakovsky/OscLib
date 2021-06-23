using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Threading;

namespace OscLib
{
    /// <summary>
    /// Represents a tree of OSC Addresses - that is, an OSC Address Space. Can receive Messages and Bundles from multiple OSC Receivers, 
    /// pattern matching if needed. Provides convenience methods for creating and managing an address tree, allowing to connect event  
    /// handlers to OSC Methods - to be invoked when a message is dispatched to a particular OSC Method. 
    /// </summary>
    public class OscAddressSpace
    {
        //TODO: add support for Protocol v. 1.1's "any address" pattern-matching thingy

        /// <summary> The default name of the root container. </summary>
        public const string RootContainerName = "root";

        #region FIELDS
        /// <summary> The name for this Address Space. </summary>
        protected readonly string _name;

        /// <summary> The root container, from which the rest of this Address Space stems. </summary>
        protected OscContainer _root;

        /// <summary> Controls access to the elements of this Address Space when adding/removing addresses. </summary>
        protected Mutex _addressSpaceAccess;

        /// <summary> Contains all OSC Receivers this Address Space is connected to. </summary>
        protected List<OscReceiver> _receivers;

        /// <summary> Controls access to the list of connected OSC Receivers. </summary>
        protected Mutex _receiversAccess;

        #endregion // FIELDS


        #region PROPERTIES
        /// <summary> The root container, from which the rest of this Address Space stems. </summary>
        public OscContainer Root { get => _root; }

        /// <summary> The name for this Address Space. </summary>
        public string Name { get => _name; }

        #endregion // PROPERTIES


        /// <summary>
        /// Creates a new OSC Address Space.
        /// </summary>
        public OscAddressSpace(string name)
        {
            _name = name;
            _root = new OscContainer(RootContainerName);
            _receivers = new List<OscReceiver>();

            _addressSpaceAccess = new Mutex();
            _receiversAccess = new Mutex();
        }


        #region CONNECTIONS
        /// <summary>
        /// Connects this Address Space to an OSC Receiver.
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
        /// Processes an incoming bundle. Also invoked when one of the connected OSC Receivers receives a bundle.
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
        /// Processes an incoming message. Also invoked when one of the connected OSC Receivers receives a message.
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
                    method?.Invoke(elementNames[elementNames.Length - 1], message.GetArguments());
                    return true;
                }

                return false;
            }

            MatchAndPerform(elementNames, false, InvokeMethod);  
            
        }

        #endregion // RECEIVING AND PROCESSING


        #region ELEMENT MANAGEMENT
        /// <summary>
        /// Subscribes an event handler to a particular OSC Method. Will do one of the two things: 
        /// <para>
        /// 1. If an OSC Method already exists at the specified address, the event handler will be subscribed to it; 
        /// </para>
        /// <para>
        /// 2. Otherwise, a new OSC Method will be created and added to this Address Space, and the event handler will 
        /// be subscribed to it. If any elements of the address don't exist in this Address Space, they will be 
        /// created and added too.
        /// </para>
        /// </summary>
        /// <param name="address"> The address to which the event handler should be added. Shouldn't contain any pattern-matching or reserved symbols except for separators. </param>
        /// <param name="handler"> The handler method that will be attached to the OSC Method, to be invoked when an appropriate OSC Message is dispatched to this address space. </param>
        /// <returns> 
        /// The OSC Method to which the handler method was added, or null if there is already an OSC Container present at the address and 
        /// it was not possible to add the handler method.
        /// </returns>
        /// <exception cref="ArgumentNullException"> Thrown one of the parameters is null or empty. </exception>
        /// <exception cref="InvalidOperationException"> Thrown when there is a non-container element in the address path. </exception>
        public OscMethod AddHandlerToMethod(OscString address, OscMethodEventHandler handler)
        {
            OscMethod added = null;

            if (address.IsNullOrEmpty())
            {
                throw new ArgumentNullException(nameof(address));
            }

            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            // get the address pattern and check it for any crap we don't need
            OscString[] pattern = address.Split(OscProtocol.Separator);

            // internal method to handle the handler handling
            OscAddressElement HandleTheHandler(OscAddressElement element)
            {
                OscMethod method = null;

                if (element == null)
                {
                    method = new OscMethod(pattern[pattern.Length - 1]);
                }
                else
                {
                    if (element is OscMethod itIs)
                    {
                        method = itIs;
                    }

                }

                if (method != null)
                {
                    method.OscMethodInvokedEvent += handler;
                }

                return method;

            }

            try
            {
                _addressSpaceAccess.WaitOne();
                added = AddElementAndPerform(pattern, HandleTheHandler) as OscMethod;

            }
            finally
            {
                _addressSpaceAccess.ReleaseMutex();
            }

            return added;

        }


        /// <summary>
        /// Creates a new OSC Method and adds it to the specified address, or returns the OSC Method that already exists at that address.
        /// </summary>
        /// <param name="address"> The address to which the OSC Method should be added. Shouldn't contain any pattern-matching or reserved symbols except for separators. </param>
        /// <returns> 
        /// The OSC Method to which the handler method was added, or null if there is already an OSC Container present at the address and 
        /// it was not possible to add the handler method.
        /// </returns>
        /// <exception cref="ArgumentNullException"> Thrown one of the parameters is null or empty. </exception>
        /// <exception cref="InvalidOperationException"> Thrown when there is a non-container element in the address path - that is, 
        /// attempting to add elements to a non-container element. </exception>
        public OscMethod AddMethod(OscString address)
        {
            OscMethod added = null;

            if (address.IsNullOrEmpty())
            {
                throw new ArgumentException("OSC Receiver ERROR: Can't add method, address pattern is invalid.");
            }
            
            OscString[] pattern = address.Split(OscProtocol.Separator);

            // internal method
            OscAddressElement MethodPlease(OscAddressElement element)
            {
                OscMethod method = null;

                if (element == null)
                {
                    method = new OscMethod(pattern[pattern.Length - 1]);
                }
                else
                {
                    if (element is OscMethod gotcha)
                    {
                        method = gotcha;
                    }
                    
                }

                return method;

            }

            try
            {
                _addressSpaceAccess.WaitOne();
                added = AddElementAndPerform(pattern, MethodPlease) as OscMethod;

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
        /// <exception cref="ArgumentNullException"> Thrown one of the parameters is null or empty. </exception>
        /// <exception cref="InvalidOperationException"> Thrown when there is a non-container element in the address path - that is, 
        /// attempting to add elements to a non-container element. </exception>
        public OscContainer AddContainer(OscString address)
        {
            OscContainer added = null;

            if (address.IsNullOrEmpty())
            {
                throw new ArgumentException("OSC Address Space ERROR: Can't add container, address pattern is invalid.");
            }

            // get the address pattern and check it for any crap we don't need
            OscString[] pattern = address.Split(OscProtocol.Separator);
           
            // the function that will deliver us the container we so crave
            OscAddressElement ShinyNewContainer(OscAddressElement element)
            {
                OscContainer container = null;

                if (element == null)
                {
                    container = new OscContainer(pattern[pattern.Length - 1]);
                }
                else
                {
                    if (element is OscContainer gotcha)
                    {
                        container = gotcha;
                    }

                }

                return container;

            }

            try
            {
                _addressSpaceAccess.WaitOne();
                added = AddElementAndPerform(pattern, ShinyNewContainer) as OscContainer;

            }
            finally
            {
                _addressSpaceAccess.ReleaseMutex();
            }

            return added;

        }


        /// <summary>
        /// Looks for an element at the specified address and retrieves it.
        /// If the address is a pattern, the first element that's matched to it will be retrieved.
        /// </summary>
        /// <param name="address"> The full address of the element. </param>
        /// <returns> The OSC Address Element corresponding to the provided address pattern, or null if nothing's been found. </returns>
        public OscAddressElement GetElementByAddress(OscString address)
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
                MatchAndPerform(elementNames, true, GetItOut);
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
        /// <param name="name"> The element's name. </param>
        /// <returns> The OSC Address Element with a matching name, or null if nothing's been found. </returns>
        public OscAddressElement GetElementByName(OscString name)
        {
            OscAddressElement returnElement = null;

            bool Gotcha(OscAddressElement element)
            {
                returnElement = element;
                return true;
            }

            try
            {
                _addressSpaceAccess.WaitOne();
                SearchAndPerform(name, true, Gotcha);
            }
            finally
            {
                _addressSpaceAccess.ReleaseMutex();
            }

            return returnElement;

        }


        /// <summary>
        /// Looks for elements with addresses that match the provided pattern, returns them as a list.
        /// </summary>
        /// <returns> A list of matching Address Elements. </returns>
        public List<OscAddressElement> GetElementsByAddress(OscString pattern)
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
                MatchAndPerform(elementNames, false, GetItOut);
            }
            finally
            {
                _addressSpaceAccess.ReleaseMutex();
            }

            return returnList;

        }


        /// <summary>
        /// Looks for elements with names that match the provided pattern, returns them as a list. 
        /// </summary>
        /// <param name="namePattern"></param>
        /// <returns> A list of found Address Elements. </returns>
        public List<OscAddressElement> GetElementsByName(OscString namePattern)
        {
            List<OscAddressElement> returnList = new List<OscAddressElement>();

            bool Gotcha(OscAddressElement element)
            {
                returnList.Add(element);
                return true;
            }

            try
            {
                _addressSpaceAccess.WaitOne();
                SearchAndPerform(namePattern, false, Gotcha);
            }
            finally
            {
                _addressSpaceAccess.ReleaseMutex();
            }

            return returnList;
            
        }


        /// <summary>
        /// Removes the specified element from the Address Space, provided it's there to begin with. 
        /// </summary>
        /// <param name="targetElement"> The element to remove. </param>
        /// <returns> Whether the element was found and removed. </returns>
        public bool RemoveElement(OscAddressElement targetElement)
        {
            bool greatSuccess = false;

            if (targetElement == null)
            {
                throw new ArgumentNullException(nameof(targetElement));
            }

            OscString[] pattern = targetElement.GetAddress().Split(OscProtocol.Separator);

            bool Remove(OscAddressElement element)
            {
                if (element == targetElement)
                {
                    greatSuccess = element.Parent.RemoveElement(element);
                }
                else
                {
                    greatSuccess = false;
                }

                return greatSuccess;

            }

            try
            {
                _addressSpaceAccess.WaitOne();
                MatchAndPerform(pattern, true, Remove);
            }
            finally
            {
                _addressSpaceAccess.ReleaseMutex();
            }

            return greatSuccess;

        }


        /// <summary>
        /// Searches for elements with names matching the provided name pattern, removes them from this Address Space.
        /// </summary>
        /// <returns> The total number of removed elements. </returns>
        public int RemoveElementsByName(OscString namePattern)
        {
            int total = 0;

            if (OscString.IsNullOrEmpty(namePattern))
            {
                throw new ArgumentNullException(nameof(namePattern));
            }

            bool Remove(OscAddressElement element)
            {
                if (element.Parent.RemoveElement(element))
                {
                    total++;
                    return true;
                }
                else
                {
                    return false;
                }
                
            }

            try
            {
                _addressSpaceAccess.WaitOne();

                SearchAndPerform(namePattern, false, Remove);
            }
            finally
            {
                _addressSpaceAccess.ReleaseMutex();
            }

            return total;

        }


        /// <summary>
        /// Removes all elements with addresses matching the provided address pattern from this Address Space. 
        /// </summary>
        /// <returns> The total number of removed elements. </returns>
        public int RemoveElementsByAddress(OscString addressPattern)
        {
            int total = 0;

            if (OscString.IsNullOrEmpty(addressPattern))
            {
                throw new ArgumentNullException(nameof(addressPattern));
            }

            OscString[] elementNames = addressPattern.Split(OscProtocol.Separator);

            bool Remove(OscAddressElement element)
            {
                if (element.Parent.RemoveElement(element))
                {
                    total++;
                    return true;
                }
                else
                {
                    return false;
                }

            }

            try
            {
                _addressSpaceAccess.WaitOne();

                MatchAndPerform(elementNames, false, Remove);

            }
            finally
            {
                _addressSpaceAccess.ReleaseMutex();
            }

            return total;

        }


        /// <summary>
        /// Returns the depth of the deepest element of this Address Space.
        /// </summary>
        public int GetMaxDepth()
        {
            int maxDepth = 1;
            int currentDepth = 1;

            OscContainer saveParent = _root;

            bool CheckDepth(OscAddressElement element)
            {
                if ((element is OscContainer container) && (container.Length > 0))
                {
                    currentDepth++;

                    if (currentDepth > maxDepth)
                    {
                        maxDepth = currentDepth;
                    }

                    saveParent = container.Parent;

                    return true;
                }

                // means we're out of the previous container
                if (element.Parent != saveParent)
                {
                    currentDepth--;
                    saveParent = element.Parent;
                }

                return true;

            }

            try
            {
                _addressSpaceAccess.WaitOne();

                SearchAndPerform("*", false, CheckDepth);

            }
            finally
            {
                _addressSpaceAccess.ReleaseMutex();
            }

            return maxDepth;

        }

        #endregion // ELEMENT MANAGEMENT


        #region SEARCH, MATCH, ETC
        /// <summary>
        /// Tries to match the provided pattern (taken as an array of element names) to addresses in this Address Space, performs the provided function on the matching elements.
        /// </summary>
        /// <param name="elementNames"> An array of OSC Address Element names that make up an address pattern. </param>
        /// <param name="performOnlyOnce"> Whether the function will only be performed on the first eligible element. </param>
        /// <param name="function"> The function to perform: 
        /// <para> -- takes one parameter (the Address Element that matches the pattern); </para>
        /// <para> -- returns a boolean (whether the function could besuccessfully performed on the provided Address Element). </para> </param>
        protected void MatchAndPerform(OscString[] elementNames, bool performOnlyOnce, Func<OscAddressElement, bool> function)
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
                        // run in reverse, in case elements are moved around or removed as a result of the performed function
                        for (int j = stack[currentLayer].Length - 1; j >= 0; j--)
                        {
                            // perform the Function(tm) if the element adheres to the pattern
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


        /// <summary>
        /// Searches the Address Space for elements that match the provided name, performs the provided function on the matching elements.
        /// </summary>
        /// <param name="namePattern"> The name pattern to match against element names. </param>
        /// <param name="performOnlyOnce"> Whether the function will only be performed on the first eligible element. </param>
        /// <param name="function"> The function to perform: 
        /// <para> -- takes one parameter (the Address Element whose name matches the pattern); </para>
        /// <para> -- returns a boolean (whether the function could besuccessfully performed on the provided Address Element). </para></param>
        protected void SearchAndPerform(OscString namePattern, bool performOnlyOnce, Func<OscAddressElement, bool> function)
        {
            OscContainer currentContainer = _root;
            int currentDepth = 1;
            int currentIndex = currentContainer.Length - 1;

            if (namePattern.IsNullOrEmpty() || namePattern.ContainsSpecialChars())
            {
                throw new ArgumentException("OSC Address Space ERROR: Cannot perform search, provided name pattern is invalid. ");
            }

            while (currentDepth > 0)
            {
                if (currentContainer == null)
                {
                    break;
                }

                // check if we're within the limits still, move up if not
                if (currentIndex < 0)
                {
                    currentDepth--;

                    if (currentContainer.Parent != null)
                    {
                        currentIndex = currentContainer.Parent.GetElementIndex(currentContainer) - 1;
                    }

                    currentContainer = currentContainer.Parent;

                    continue;
                }

                // perform the function on the current element
                OscAddressElement currentElement = currentContainer[currentIndex];
                currentIndex--;

                if (currentElement.Name.PatternMatch(namePattern))
                {
                    if (function(currentElement) && performOnlyOnce)
                    {
                        break;
                    }

                }

                // check if current element still exists (in case it's got removed by the function); if it does check if it's a container; if it is enter it
                if (currentContainer.ContainsElement(currentElement))
                {
                    if ((currentElement is OscContainer newContainer) && (newContainer.Length > 0))
                    {
                        currentDepth++;
                        currentContainer = newContainer;
                        currentIndex = currentContainer.Length - 1;
                    }

                }           

            }

        }


        /// <summary>
        /// Adds an element to the provided address (adding containers as needed if need be) and does stuff to it, all via the provided function. 
        /// </summary>
        /// <param name="elementNames"> An array of OSC Address Element names that make up an address pattern. The last name will be that of the target element. </param>
        /// <param name="function"> This function will be performed when the method reaches the final container and assesses whether it contains the target element. 
        /// One of the following two things will happen at this point (the function should be able to tell which of these two scenarios is occuring by the fact that it will be passed a null in the second one):
        /// <para> 
        /// 1. If that container does contain the element with the target name, the function will take it as an argument and will Do Stuff to it. 
        /// </para>
        /// <para> 
        /// 2. If that container doesn't contain the element, the function will create a new element, then Do Stuff to it, then add it to the container.
        /// </para>
        /// If the function wass unsuccessful in its performance, it should return a null itself. </param>
        /// <exception cref="ArgumentException"> Thrown when one of element names contains special symbols. </exception>
        /// <exception cref="InvalidOperationException"> Thrown when there is a non-container element in the address path - that is, attempting to add elements to a non-container element. </exception>
        /// <returns> The element it added, or null if it was unsuccessful. </returns>
        protected OscAddressElement AddElementAndPerform(OscString[] elementNames, Func<OscAddressElement, OscAddressElement> function)
        {

            OscContainer currentContainer = _root;

            OscAddressElement added = null;

            for (int i = 0; i < elementNames.Length; i++)
            {
                // if we're not at the last bit of the address, let's find an appropriate container, or create a new one 
                if (i != elementNames.Length - 1)
                {
                    if (elementNames[i].ContainsPatternMatching() || elementNames[i].ContainsSpecialChars())
                    {
                        throw new ArgumentException("OSC Address Space ERROR: Cannot add method, address pattern contains invalid symbols.");
                    }

                    if (currentContainer[elementNames[i]] is OscContainer newContainer)
                    {
                        currentContainer = newContainer;
                    }
                    else if (currentContainer[elementNames[i]] is OscMethod)
                    {
                        throw new InvalidOperationException("OSC Address Space ERROR: Cannot append address elements to a method.");
                    }
                    else
                    {
                        newContainer = new OscContainer(elementNames[i]);
                        currentContainer.AddElement(newContainer);
                        currentContainer = newContainer;
                    }

                }
                else
                {
                    if (!currentContainer.ContainsElement(elementNames[i]))
                    {
                        added = function(null);

                        if (added != null)
                        {
                            currentContainer.AddElement(added);
                        }

                    }
                    else
                    {
                        added = function(currentContainer[elementNames[i]]);                     
                    }

                }

            }

            return added;

        }

        #endregion


        /// <summary>
        /// Prints the name of this OSC Address Space and lists all connected Receivers.
        /// </summary>
        public override string ToString()
        {
            StringBuilder returnString = new StringBuilder("OSC Address Space:\nName: ");
            returnString.Append(_name);
            returnString.Append('\n');
            returnString.Append("OSC Links connected: ");

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
        /// Prints the entire OSC Address Space formatted as an address tree. 
        /// </summary>
        /// <returns></returns>
        public string PrintAddressTree()
        {
            StringBuilder returnString = new StringBuilder(this.ToString());

            try
            {
                _addressSpaceAccess.WaitOne();
                int currentLayer = 0;

                List<OscContainer> currentPath = new List<OscContainer>();
                List<int> indices = new List<int>();

                // add root
                currentPath.Add(_root);
                indices.Add(0);

                returnString.Append('\n');
                returnString.Append(_root.ToString());
                returnString.Append('\n');

                while (currentLayer >= 0)
                {
                    // index of -1 for the current layer indicates that we're done with this particular layer and can safely go back
                    if (indices[currentLayer] >= currentPath[currentLayer].Length)
                    {
                        // append the spaces to designate the current depth
                        returnString.Append(OscUtil.GetRepeatingChar(' ', (currentLayer) * 4));
                        returnString.Append("----End of ");
                        returnString.Append(currentPath[currentLayer].Name);
                        returnString.Append('\n');
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
