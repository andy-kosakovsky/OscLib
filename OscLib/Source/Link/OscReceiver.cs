using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Threading;

namespace OscLib
{
    /// <summary>
    /// Implements an OSC Address Space, associating C# method delegates with OSC Methods. Processes messages and bundles coming from the attached OSC Link, pattern matching if needed.
    /// <para> To Do: implement a buffer and delay system to properly adhere OSC Protocol's rules on timestamps, etc. Currently all bundles will be processed as they come, unless their timestamp is in the past. </para>
    /// </summary>
    public class OscReceiver
    {
        /// <summary> The default name of the root container. </summary>
        public const string RootContainerName = "root";

        /// <summary> The root container, from which all the larger OSC Address Space stems. </summary>
        protected OscContainer _root;

        /// <summary> Controls access to the OSC Address Space when trying to add/remove addresses. </summary>
        protected Mutex _addressSpaceAccess;

        /// <summary> Controls access to the list of connected links. </summary>
        protected Mutex _connectedLinksAccess;

        /// <summary> Contains all links this receiver is connected to. </summary>
        protected List<OscLink> _connectedLinks;

        /// <summary> The root container, from which the rest of the OSC Address Space stems. </summary>
        public OscContainer Root { get => _root; }

        /// <summary>
        /// Creates a new OSC Receiver.
        /// </summary>
        public OscReceiver()
        {
            _root = new OscContainer(RootContainerName);
            _connectedLinks = new List<OscLink>();

            _addressSpaceAccess = new Mutex();
            _connectedLinksAccess = new Mutex();
        }

        /// <summary>
        /// Connects this Receiver to an OSC Link.
        /// </summary>
        /// <param name="link">An OSC Link to receive messages from. Make sure its "message/bundle received" events are on, otherwise nothing will happen.</param>
        /// <exception cref="ArgumentNullException">Thrown if the provided OSC Link is null.</exception>
        public void Connect(OscLink link)
        {
            if (link == null)
            {
                throw new ArgumentNullException(nameof(link));
            }

            try
            {
                _connectedLinksAccess.WaitOne();

                if (!_connectedLinks.Contains(link))
                {
                    _connectedLinks.Add(link);
                    link.BundlesReceived += ReceiveBundles;
                    link.MessageReceived += ReceiveMessage;
                }
                
            }
            finally
            {
                _connectedLinksAccess.ReleaseMutex();
            }
        }

        public void Disconnect(OscLink link)
        {
            if (link == null)
            {
                throw new ArgumentNullException(nameof(link));
            }

            try
            {
                _connectedLinksAccess.WaitOne();

                if (!_connectedLinks.Contains(link))
                {
                    _connectedLinks.Remove(link);
                    link.BundlesReceived -= ReceiveBundles;
                    link.MessageReceived -= ReceiveMessage;
                }

            }
            finally
            {
                _connectedLinksAccess.ReleaseMutex();
            }


        }

        
        /// <summary>
        /// Processes incoming message batches, invoking the appropriate OSC Methods.
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

            OscString[] pattern = message.AddressPattern.Split(OscProtocol.SymbolAddressSeparator);

            // the layer of the pattern that contains method name (0 will be root)
            int methodLayer = pattern.Length - 1;

            int currentLayer = 0;

            object[] arguments = message.Arguments;

            // array for the container stack. its length equals the maximum depth at which we'll be checking
            OscContainer[] stack = new OscContainer[pattern.Length];
            // a span for layer indices
            int[] indices = new int[pattern.Length];

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
                    if (!pattern[currentLayer].ContainsReservedSymbols)
                    {
                        if (stack[currentLayer][pattern[currentLayer]] is OscMethod part)
                        {
                            part.Invoke(arguments);
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
                                if (method.Name.PatternMatch(pattern[currentLayer]))
                                {
                                    method.Invoke(arguments);
                                }
                            }
                        }
                        // go up a layer
                        indices[currentLayer] = -1;
                        currentLayer--;

                    }

                }
                else
                {
                    if (!pattern[currentLayer].ContainsReservedSymbols)
                    {
                        if (stack[currentLayer][pattern[currentLayer]] is OscContainer container)
                        {
                            // it's the only container we need, so index for the current layer should be set to -1
                            indices[currentLayer] = -1;

                            // then, let's go up a layer, and prepare everything for the next cycle 
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
                                if (container.Name.PatternMatch(pattern[currentLayer]))
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
        /// Processes an incoming batch of bundles. Invoked when the connected OSC Link receives bundles.
        /// </summary>
        /// <param name="bundles"> A batch of OSC bundles to process. </param>
        /// <param name="receivedFrom"> The IP end point from which the bundles were received. </param>
        public virtual void ReceiveBundles(OscBundle[] bundles, IPEndPoint receivedFrom)
        {
            try
            {
                _addressSpaceAccess.WaitOne();

                for (int i = 0; i < bundles.Length; i++)
                {
                    Process(bundles[i].Messages);
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
        /// Adds an OSC Method to the specified address in the OSC Address Space.
        /// </summary>
        /// <param name="addressPattern"> The address of this OSC Method. Shouldn't contain any reserved symbols except for separators. The last part of the pattern is used as the method's name. </param>
        /// <param name="method"> The method delegate that will be attached to the OSC Method, to be invoked when the method is triggered by an OSC message. </param>
        /// <exception cref="ArgumentException"> Thrown when the address pattern is invalid. </exception>
        public void AddMethod(OscString addressPattern, OscMethodDelegate method)
        {
            if (addressPattern.Length < 1)
            {
                throw new ArgumentException("OSC Receiver ERROR: Can't add method, address pattern is invalid.");
            }

            try
            {
                _addressSpaceAccess.WaitOne();

                // get the address pattern and check it for any crap we don't need
                OscString[] pattern = addressPattern.Split(OscProtocol.SymbolAddressSeparator);

                OscContainer container = _root;

                for (int i = 0; i < pattern.Length; i++)
                {
                    if (pattern[i].ContainsReservedSymbols)
                    {
                        throw new ArgumentException("OSC Receiver ERROR: Can't add method, address pattern contains invalid symbols");
                    }

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
                            container.AddPart(newContainer);
                            container = newContainer;
                        }
                    }
                    else
                    {
                        if (!container.ContainsPart(pattern[i]))
                        {
                            container.AddPart(new OscMethod(pattern[i], method));
                        }
                        else
                        {
                            // delete the part that is there and overwrite it with a new one
                            container.RemovePart(pattern[i]);
                            container.AddPart(new OscMethod(pattern[i], method));
                        }

                    }

                }

            }
            finally
            {
                _addressSpaceAccess.ReleaseMutex();
            }

        }

        /// <summary>
        /// Adds an OSC Container to the specified address in the OSC Address Space.
        /// </summary>
        /// <param name="addressPattern"> The address of this OSC Container. Shouldn't contain any reserved symbols except for separators. The last part of the pattern is used as the container's name. </param>
        /// <exception cref="ArgumentException"> Thrown when the address pattern is invalid. </exception>
        public void AddContainer(OscString addressPattern)
        {
            if (addressPattern.Length < 1)
            {
                throw new ArgumentException("OSC Receiver ERROR: Can't add method, address pattern is invalid.");
            }

            try
            {
                _addressSpaceAccess.WaitOne();

            // get the address pattern and check it for any crap we don't need
            OscString[] pattern = addressPattern.Split(OscProtocol.SymbolAddressSeparator);

            OscContainer container = _root;

                for (int i = 0; i < pattern.Length; i++)
                {
                    if (pattern[i].ContainsReservedSymbols)
                    {
                        throw new ArgumentException("OSC Receiver ERROR: Can't add method, address pattern contains invalid symbols");
                    }

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
                            container.AddPart(newContainer);
                            container = newContainer;
                        }
                    }
                    else
                    {
                        if (!container.ContainsPart(pattern[i]))
                        {
                            container.AddPart(new OscContainer(pattern[i]));
                        }
                        else
                        {
                            // delete the part that is there and overwrite it with a new one
                            container.RemovePart(pattern[i]);
                            container.AddPart(new OscContainer(pattern[i]));
                        }

                    }

                }

            }
            finally
            {
                _addressSpaceAccess.ReleaseMutex();
            }

        }


        /// <summary>
        /// Removes the specified address (be it a container or a method) from the address space.
        /// <para>Warning: pattern matching not yet implemented, attempts will cause an exception.</para>
        /// </summary>
        /// <param name="addressPattern"></param>
        public void RemoveAddress(OscString addressPattern)
        {
            if (addressPattern.Length < 1)
            {
                throw new ArgumentException("OSC Receiver ERROR: Can't remove address, address pattern is invalid.");
            }

            try
            {
                _addressSpaceAccess.WaitOne();

                // get the address pattern and check it for any crap we don't need
                OscString[] pattern = addressPattern.Split(OscProtocol.SymbolAddressSeparator);

                OscContainer container = _root;

                for (int i = 0; i < pattern.Length; i++)
                {
                    // bumping into a reserved symbol at this stage means that there was an attempt at pattern matching, and we can't have that just yet
                    if (pattern[i].ContainsReservedSymbols)
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
                        if (!container.ContainsPart(pattern[i]))
                        {
                            throw new ArgumentException("OSC Receiver ERROR: Can't delete address " + addressPattern + ", container " + container.Name + " doesn't contain an address " + pattern[i]);
                        }
                        else
                        {
                            // delete the part
                            container.RemovePart(pattern[i]);                           
                        }

                    }

                }

            }
            finally
            {
                _addressSpaceAccess.ReleaseMutex();
            }

        }


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
                        indices.RemoveAt(currentLayer);
                        currentPath.RemoveAt(currentLayer);
                        currentLayer--;
                        continue;
                    }

                    // append the spaces to designate the current depth
                    for (int i = 0; i < (currentLayer + 1) * 4; i++)
                    {
                        returnString.Append(' ');
                    }

                                                          
                    if (currentPath[currentLayer][indices[currentLayer]] is OscContainer container)
                    {
                        returnString.Append("CONTAINER: ");
                        returnString.Append(container.Name.ToString());
                        returnString.Append('(');
                        returnString.Append(container.Length);
                        returnString.Append(')');
                        returnString.Append('\n');

                        indices[currentLayer]++;
                        // then, let's go up a layer, and prepare everything for the next cycle 
                        currentLayer++;
                        currentPath.Add(container);
                        indices.Add(0);
                    }
                    else if (currentPath[currentLayer][indices[currentLayer]] is OscMethod method)
                    {
                        returnString.Append("METHOD: ");
                        returnString.Append(method.Name.ToString());
                        returnString.Append(", delegate: ");
                        returnString.Append(method.DelegateName);
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
