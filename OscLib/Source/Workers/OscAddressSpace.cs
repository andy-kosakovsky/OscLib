using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Threading;

namespace OscLib
{
    // TODO: Bit of a mess going on here, get the address space iteration stuff into a separate method or something
    // TODO: related to that, implement GetElement(s) methods
    // TODO: Make it work with method events

    /// <summary>
    /// Implements an OSC Address Space, associating C# method delegates with OSC Methods. Processes messages and bundles coming from the attached OSC Receivers, pattern matching if needed.
    /// Can handle multiple OSC Receivers.
    /// </summary>
    public class OscAddressSpace
    {
        /// <summary> The default name of the root container. </summary>
        public const string RootContainerName = "root";

        /// <summary> The root container, from which the rest of the OSC Address Space stems. </summary>
        protected OscContainer _root;

        /// <summary> Controls access to the OSC Address Space when trying to add/remove addresses. </summary>
        protected Mutex _addressSpaceAccess;

        /// <summary> Contains all OSC Receivers this Address Space is connected to, and the associated converters. </summary>
        protected List<OscReceiver> _receivers;

        /// <summary> Controls access to the list of connected links. </summary>
        protected Mutex _receiversAccess;

        /// <summary> The root container, from which the rest of the OSC Address Space stems. </summary>
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
        /// Disconnects an OSC Receiver from this Address Space (provided it was connected in the first place). 
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
        public virtual void ReceiveBundle(OscBundle bundle, IPEndPoint receivedFrom)
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
        public virtual void ReceiveMessage(OscMessage message, IPEndPoint receivedFrom)
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
        protected virtual void Process(OscMessage[] messages)
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
        protected virtual void Process(OscMessage message)
        {
            // get pattern elements

            OscString[] elementNames = message.AddressPattern.Split(OscProtocol.Separator);

            // the layer of the pattern that contains method name (0 will be root)
            int methodLayer = elementNames.Length - 1;

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
                if (currentLayer == methodLayer)
                {
                    // if we don't have any reserved symbols, that means there should be only one method adhering to the pattern
                    if (!elementNames[currentLayer].ContainsPatternMatching())
                    {
                        if (stack[currentLayer][elementNames[currentLayer]] is OscMethod part)
                        {
                            part.Invoke(this, message.GetArguments());
                        }

                        // go up a layer
                        indices[currentLayer] = -1;
                        currentLayer--;

                    }
                    else
                    {
                        for (int j = 0; j < stack[currentLayer].Length; j++)
                        {
                            if (stack[currentLayer][j] is OscMethod method)
                            {
                                if (method.Name.PatternMatch(elementNames[currentLayer]))
                                {
                                    method.Invoke(this, message.GetArguments());
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

        #endregion // RECEIVING AND PROCESSING


        #region ELEMENT MANAGEMENT

        /// <summary>
        /// Adds a method to the Address Space, connecting the specified address to the specified OSC Method. If the address doesn't exist, it will be created.
        /// </summary>
        /// <param name="address"> The address to which the method should be added. Shouldn't contain any reserved symbols except for separators. </param>
        /// <param name="method"> The method delegate that will be attached to the OSC Method, to be invoked when the method is triggered by an OSC message. </param>
        /// <returns> The OSC Method to which the method was added, or null if there is already an OSC Container present at the address. </returns>
        /// <exception cref="ArgumentException"> Thrown when the address pattern is invalid. </exception>
        public virtual OscMethod AddMethod(OscString address, OscMethodHandler method)
        {
            OscMethod added = null;

            if (address.Length < 1)
            {
                throw new ArgumentException("OSC Receiver ERROR: Can't add method, address pattern is invalid.");
            }

            if (method == null)
            {
                throw new ArgumentNullException(nameof(method));
            }

            try
            {
                _addressSpaceAccess.WaitOne();

                // get the address pattern and check it for any crap we don't need
                OscString[] pattern = address.Split(OscProtocol.Separator);

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
        /// Adds an OSC Container to the specified address in the OSC Address Space. If the address doesn't exist, it will be created.
        /// </summary>
        /// <param name="address"> The address of this OSC Container. Shouldn't contain any reserved symbols except for separators. </param>
        /// <returns> The added container, or the existing container at the specified address (if one does exist already), or null if there is already an OSC Method at the specified address instead. </returns>
        /// <exception cref="ArgumentException"> Thrown when the address pattern is invalid. </exception>
        public virtual OscContainer AddContainer(OscString address)
        {

            OscContainer added = null;

            if (address.Length < 1)
            {
                throw new ArgumentException("OSC Receiver ERROR: Can't add method, address pattern is invalid.");
            }

            try
            {
                _addressSpaceAccess.WaitOne();

            // get the address pattern and check it for any crap we don't need
            OscString[] pattern = address.Split(OscProtocol.Separator);

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


        public OscAddressElement GetElement(OscString address)
        {
            throw new NotImplementedException();
        }


        public OscAddressElement[] GetElements(OscString pattern)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Removes the specified address (be it a container or a method) from the address space.
        /// <para>Warning: pattern matching not yet implemented, attempts will cause an exception.</para>
        /// </summary>
        /// <param name="addressPattern"></param>
        public virtual void RemoveElement(OscString addressPattern)
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
                            throw new ArgumentException("OSC Receiver ERROR: Can't delete address " + addressPattern + ", container " + container.Name + " doesn't contain a container named " + pattern[i]);
                        }
                    }
                    else
                    {
                        if (!container.ContainsElement(pattern[i]))
                        {
                            throw new ArgumentException("OSC Receiver ERROR: Can't delete address " + addressPattern + ", container " + container.Name + " doesn't contain an address " + pattern[i]);
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
        /// Prints the entire OSC Address Space as a tree.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
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

                _receiversAccess.WaitOne();

                returnString.Append('\n');
                returnString.Append("OSC Links connected:\n");

                for (int i = 0; i < _receivers.Count; i++)
                {
                    returnString.Append('[');
                    returnString.Append(i);
                    returnString.Append(']');
                    returnString.Append(": ");
                    returnString.Append(_receivers[i].ToString());
                    returnString.Append('\n');
                }

            }
            finally
            {
                _addressSpaceAccess.ReleaseMutex();
                _receiversAccess.ReleaseMutex();

            }

            return returnString.ToString();

        }

    }

}
