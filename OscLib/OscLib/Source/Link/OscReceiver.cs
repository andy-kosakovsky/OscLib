using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Reflection;
using System.Diagnostics;
using System.Threading;

namespace OscLib
{
    public class OscReceiver
    {
        private const string _rootContainerName = "root";

        protected OscContainer _root;

        // mutex for accessing the address space
        protected Mutex _accessMutex;

        public OscContainer Root { get => _root; }


        public OscReceiver(OscLink link)
        {
            _root = new OscContainer(_rootContainerName);
            _accessMutex = new Mutex();

            link.MessageReceivedAsData += ReceiveMessage;
            link.BundleReceivedAsData += ReceiveBundle;
        }

        
        /// <summary>
        /// Processes incoming messages, invoking the appropriate OSC Methods
        /// </summary>
        /// <param name="messages"></param>
        private void Process(OscMessage[] messages)
        {
            // get pattern elements

            for (int i = 0; i < messages.Length; i++)
            {
                Process(messages[i]);
            }

        }


        /// <summary>
        /// Processes incoming messages, invoking the appropriate OSC Methods
        /// </summary>
        /// <param name="messages"></param>
        private void Process(OscMessage message)
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
            Span<int> indices = stackalloc int[pattern.Length];

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

        public void ReceiveBundle(OscBundle[] bundles, IPEndPoint receivedFrom)
        {
            try
            {
                _accessMutex.WaitOne();

                for (int i = 0; i < bundles.Length; i++)
                {
                    Process(bundles[i].Messages);
                }
            }
            finally
            {
                _accessMutex.ReleaseMutex();
            }

        }

        public void ReceiveMessage(OscMessage message, IPEndPoint receivedFrom)
        {
            try
            {
                _accessMutex.WaitOne();
                Process(message);
            }
            finally
            {
                _accessMutex.ReleaseMutex();
            }
        }

        /// <summary>
        /// Adds an OSC Method to the provided address
        /// </summary>
        /// <param name="addressPattern"></param>
        /// <param name="method"></param>
        public void AddMethod(OscString addressPattern, OscMethodDelegate method)
        {
            try
            {
                _accessMutex.WaitOne();

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
                _accessMutex.ReleaseMutex();
            }

        }

        /// <summary>
        /// Creates an OSC Method Container at the provided address
        /// </summary>
        /// <param name="addressPattern"></param>
        public void AddContainer(OscString addressPattern)
        {
            try
            {
                _accessMutex.WaitOne();

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
                _accessMutex.ReleaseMutex();
            }

        }

        public void RemoveAddress(OscString pattern)
        {


        }

        /// <summary>
        /// Will print the address tree
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder returnString = new StringBuilder("RECEIVER ADDRESS SPACE:\n");
            
            try
            {
                _accessMutex.WaitOne();

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
                _accessMutex.ReleaseMutex();

            }

            return returnString.ToString();

        }

    }

}
