using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace OscLib
{



    /// <summary>
    /// Implements a multi-priority sender queue to work with an OSC Link, for message bundling and orderly sending. OSC Link needs to be in targeted mode when operating.
    /// </summary>
    /// <typeparam name="Packet"> The particular type of the OSC binary packet used with this Sender. Should implement the IOscPacketBinary interface. </typeparam>
    public class OscSender<Packet> where Packet : IOscPacketBytes
    {
        #region FIELDS
        /// <summary> OSC Link in use with this Sender. Needs to be in targeted mode. </summary>
        protected OscLink _oscLink;

        /// <summary> The total amount of priority levels in this Sender's packet heap. </summary>
        protected int _packetHeapPriorityLayersTotal;

        /// <summary> The length of the heap check cycle of this Sender - that is, the time between heap checks, in milliseconds. Shoudln't be less than 1. </summary>
        protected int _cycleLengthMs;

        /// <summary> Should this Sender bundle the packets before sending, or just send them as separate messages as they come. </summary>
        protected bool _bundlePacketsBeforeSending;

        /// <summary> Whether this Sender is currently active. </summary>
        protected bool _isActive;

        /// <summary> The packet heap, holding all the packets before they are processed and sent (or not). Implements several priority levels. </summary>
        protected List<Packet>[] _packetHeap;

        /// <summary> Used to facilitate orderly access to the packet heap. </summary>
        protected Mutex _packetHeapAccessMutex;

        /// <summary> Holds the binary data pertaining to the current heap check cycle. </summary>
        protected byte[] _cycleBinaryDataHolder;

        /// <summary> Maximum amount of data sent per packet, in bytes. As per UDP spec, sending more than 508 bytes per packet is not advisable. </summary> 
        protected int _packetMaxLength;

        /// <summary> The Task that performs the packet heap checks and manages it, sending the packets out when necessary. </summary>
        protected Task _processPacketHeapTask;

        // delegates
        /// <summary> Checks whether the packet should be sent in the current cycle. </summary> 
        protected OscPacketReadyChecker<Packet> _packetReadyCheckerMethod;

        /// <summary> Checks whether the packet should be removed in the current cycle. </summary> 
        protected OscPacketRemover<Packet> _packetRemoverMethod;

        #endregion


        #region PROPERTIES
        /// <summary> Whether this Sender is currently active. </summary>
        public bool IsActive { get => _isActive; }

        /// <summary> Shows the current status of the task processing the packet heap. </summary>
        public TaskStatus ProcessPacketHeapTaskStatus { get => _processPacketHeapTask.Status; }

        
        /// <summary> Should this Sender bundle the packets before sending, or just send them as separate messages as they come. </summary>
        public bool BundlePacketsBeforeSending { get => _bundlePacketsBeforeSending; set => _bundlePacketsBeforeSending = value; }

        /// <summary> The length of the heap check cycle of this Sender - that is, the time between heap checks, in milliseconds. Minimum 1 ms. </summary>
        public int CycleLengthMs 
        { 
            get => _cycleLengthMs;  
            
            set
            {
                if (value > 1)
                    _cycleLengthMs = value;
                else
                    _cycleLengthMs = 1;
            }
        
        }

        /// <summary> Controls the maximum allowed length for outgoing OSC data packets. Minimum 128 bytes. </summary>
        public int PacketMaxLength 
        {
            get => _packetMaxLength;
            set
            {
                int newLength = value;

                // ensure the minimum
                if (newLength < 128)
                {
                    newLength = 128;
                }

                if (newLength != _packetMaxLength)
                {
                    try
                    {
                        // just to be safe, let's wait until cycle bundle data holder is not in use
                        _packetHeapAccessMutex.WaitOne();
                        _packetMaxLength = newLength;
                        _cycleBinaryDataHolder = new byte[_packetMaxLength];
                    }
                    finally
                    {
                        _packetHeapAccessMutex.ReleaseMutex();
                    }

                }

            }

        }

        #endregion

        #region EVENTS

        /// <summary> Invoked when an exception happens inside the send task, hopefully preventing it from stopping </summary>
        public event OscTaskExceptionHandler SendTaskExceptionRaised;

        #endregion

        #region CONSTRUCTORS

        /// <summary>
        /// Creates a new OSC Sender.
        /// </summary>
        /// <param name="packetMaxLength"> Maximum amount of data sent per packet, in bytes. As per UDP spec, sending more than 508 bytes per packet is not advisable. </param>
        /// <param name="cycleLengthMs"> The length of the heap check cycle of this Sender - that is, the time between heap checks, in milliseconds. Shoudln't be less than 1. </param>
        /// <param name="priorityLayersTotal"> The total amount of priority levels in this Sender's packet heap. </param>
        /// <param name="bundlePacketsBefureSending"> Should this Sender bundle the packets before sending, or just send them as separate messages as they come. </param>
        public OscSender(int packetMaxLength = 508, int cycleLengthMs = 1, int priorityLayersTotal = 1, bool bundlePacketsBefureSending = true)
        {
            _packetHeapAccessMutex = new Mutex();
            _packetMaxLength = packetMaxLength;
            _cycleBinaryDataHolder = new byte[_packetMaxLength];

            // get default methods to act as delegates
            _packetReadyCheckerMethod = DefaultMessageChecker;
            _packetRemoverMethod = DefaultMessageDeleter;

            // set options
            if (priorityLayersTotal > 1)
                _packetHeapPriorityLayersTotal = priorityLayersTotal;
            else
                _packetHeapPriorityLayersTotal = 1;

            _cycleLengthMs = cycleLengthMs;
            _bundlePacketsBeforeSending = bundlePacketsBefureSending;

        }

        #endregion

        #region METHODS

        /// <summary>
        /// Activates the Sender and connects it to an OSC Link. 
        /// </summary>
        /// <param name="oscLink"> The OSC Link this Sender will be sending packets through. </param>
        /// <exception cref="ArgumentNullException"> Is thrown when the provided OSC Link is null. </exception>
        public void Activate(OscLink oscLink)
        {
            if (!_isActive)
            {

                if (oscLink == null)
                    throw new ArgumentNullException(nameof(oscLink));

                // get the link
                _oscLink = oscLink;

                // initialize data heap with requested number of priority levels
                _packetHeap = new List<Packet>[_packetHeapPriorityLayersTotal];

                for (int i = 0; i < _packetHeapPriorityLayersTotal; i++)
                    _packetHeap[i] = new List<Packet>();

                _isActive = true;

                _processPacketHeapTask = Task.Run(HeapProcessingCycle);

            }

        }


        /// <summary>
        /// Deactivates the sender, cancelling the heap processing task and disattaching the sender from the OSC Link.
        /// </summary>
        public void Deactivate()
        {
            if (_isActive)
            {
                _isActive = false;
                _processPacketHeapTask.Wait();
                _oscLink = null;

            }

        }


        /// <summary>
        /// Provides the method delegate for checking if a packet is eligible to be sent.
        /// </summary>
        /// <param name="readyCheckerMethod"> The method to be used for checking packets. </param>
        public void SetPacketReadyCheckerMethod(OscPacketReadyChecker<Packet> readyCheckerMethod)
        {
            _packetReadyCheckerMethod = readyCheckerMethod;
        }


        /// <summary>
        /// Provides the method delegate for checking if a packet needs to be removed from the heap.
        /// </summary>
        /// <param name="removerMethod"> The method to be used for removing packets. </param>
        public void SetPacketRemoverMethod(OscPacketRemover<Packet> removerMethod)
        {
            _packetRemoverMethod = removerMethod;
        }


        /// <summary>
        /// Gets the binary data from the OSC packet and immediately sends it through the OSC Link as a message.
        /// </summary>
        /// <param name="packet"> A reference to the OSC binary data packet to be sent. </param>
        /// <exception cref="InvalidOperationException"> Thrown when attempting to send data while the sender is not active. </exception>
        /// <exception cref="ArgumentOutOfRangeException"> Thrown when the size of the packet is too large to be sent. </exception>
        public void SendOscPacket(Packet packet)
        {
            if (!_isActive)
            {
                throw new InvalidOperationException("SC Server Sender Error: Can't send data, sender is not active.");
            }

            if (packet.Length > _packetMaxLength)
            {
                throw new ArgumentOutOfRangeException(nameof(packet), "OSC Sender Error: Too much OSC data to safely send in one message.");
            }

            if (_oscLink.Mode == LinkMode.Targeted)
            {
                _oscLink.SendToTarget(packet);
            }
            else
            {
                throw new InvalidOperationException("OSC Sender Error: Can't send data, sender is not active, OSC Link is in wrong mode.");
            }
        }


        /// <summary>
        /// Inserts the packet to the beginning of the heap at the specified priority level, passing the packet by reference.
        /// <para> Important to note: heap levels are processed tail-to-head, so the packet added by this method will be last in line.</para>
        /// <para> Also, this method is slower than adding to heap tail. </para>
        /// </summary>
        /// <param name="packet"> The OSC binary data packet. </param>
        /// <param name="priorityLevel"> The priority level of the packet, with 0 being the highest priority. </param>
        /// <exception cref="InvalidOperationException"> Thrown when attempting to add data while the sender is not active. </exception>
        /// <exception cref="ArgumentOutOfRangeException"> Thrown when the size of the binary data packet is too large to be sent. </exception>
        public void AddOscPacketToHeapHead(ref Packet packet, int priorityLevel = 0)
        {
            if (!_isActive)
            {
                throw new InvalidOperationException("SC Server Sender Error: Sender is not active.");
            }

            if (packet.Length > _packetMaxLength)
            {
                throw new ArgumentOutOfRangeException(nameof(packet), "OSC Sender Error: Too much OSC data to safely send in one message.");
            }

            int priority;

            // make sure priority level is within bounds
            if (priorityLevel < 0)
            {
                priority = 0;
            }
            else if (priorityLevel >= (_packetHeapPriorityLayersTotal - 1))
            {
                priority = _packetHeapPriorityLayersTotal - 1;
            }
            else
            {
                priority = priorityLevel;
            }

            // finally, add data to heap
            try
            {
                _packetHeapAccessMutex.WaitOne();
                _packetHeap[priority].Insert(0, packet);
            }
            finally
            {
                _packetHeapAccessMutex.ReleaseMutex();
            }

        }


        /// <summary>
        /// Inserts the packet to the beginning of the heap at the specified priority level, passing the packet directly.
        /// <para> Important to note: heap levels are processed tail-to-head, so the packet added by this method will be last in line.</para>
        /// <para> Also, this method is slower than adding to heap tail. </para>
        /// </summary>
        /// <param name="packet"> The OSC binary data packet. </param>
        /// <param name="priorityLevel"> The priority level of the packet, with 0 being the highest priority. </param>
        /// <exception cref="InvalidOperationException"> Thrown when attempting to add data while the sender is not active. </exception>
        /// <exception cref="ArgumentOutOfRangeException"> Thrown when the size of the binary data packet is too large to be sent. </exception>
        public void AddOscPacketToHeapHead(Packet packet, int priorityLevel = 0)
        {
            if (!_isActive)
            {
                throw new InvalidOperationException("SC Server Sender Error: Sender is not active.");
            }

            if (packet.Length > _packetMaxLength)
            {
                throw new ArgumentOutOfRangeException(nameof(packet), "OSC Sender Error: Too much OSC data to safely send in one message.");
            }

            int priority;

            // make sure priority level is within bounds
            if (priorityLevel < 0)
            {
                priority = 0;
            }
            else if (priorityLevel >= (_packetHeapPriorityLayersTotal - 1))
            {
                priority = _packetHeapPriorityLayersTotal - 1;
            }
            else
            {
                priority = priorityLevel;
            }

            // finally, add data to heap
            try
            {
                _packetHeapAccessMutex.WaitOne();
                _packetHeap[priority].Insert(0, packet);
            }
            finally
            {
                _packetHeapAccessMutex.ReleaseMutex();
            }

        }


        /// <summary>
        /// Adds the OSC packet to the end of the heap at the specified priority level, passing the OSC packet by reference.
        /// <para> Important to note: heap levels are processed tail-to-head, so the packet added by this method will be first in line. </para>
        /// <para> This method is faster than adding to heap head. </para>
        /// </summary>        
        /// <param name="packet"> The OSC binary data packet. </param>
        /// <param name="priorityLevel"> The priority level of the packet, with 0 being the highest priority. </param>
        /// <exception cref="InvalidOperationException"> Thrown when attempting to add data while the sender is not active. </exception>
        /// <exception cref="ArgumentOutOfRangeException"> Thrown when the size of the binary data packet is too large to be sent. </exception>
        public void AddOscPacketToHeapTail(ref Packet packet, int priorityLevel = 0)
        {

            if (!_isActive)
            {
                throw new InvalidOperationException("SC Server Sender Error: Sender is not running.");
            }

            if (packet.Length > _packetMaxLength)
            {
                throw new ArgumentOutOfRangeException("OSC Sender Error: Too much OSC data to safely send in one message.");
            }


            int priority;

            // make sure priority level is within bounds
            if (priorityLevel < 0)
            {
                priority = 0;
            }
            else if (priorityLevel >= (_packetHeapPriorityLayersTotal - 1))
            {
                priority = _packetHeapPriorityLayersTotal - 1;
            }
            else
            {
                priority = priorityLevel;
            }

            // finally, add data to heap
            try
            {
                _packetHeapAccessMutex.WaitOne();
                _packetHeap[priority].Add(packet);
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                _packetHeapAccessMutex.ReleaseMutex();
            }

        }


        /// <summary>
        /// Adds the OSC packet to the end of the heap at the specified priority level, passing the packet directly.
        /// <para> Important to note: heap levels are processed tail-to-head, so the packet added by this method will be first in line.</para>
        /// <para> This method is faster than adding to heap head. </para>
        /// </summary>        
        /// <param name="packet"> The OSC binary data packet. </param>
        /// <param name="priorityLevel"> The priority level of the packet, with 0 being the highest priority. </param>
        /// <exception cref="InvalidOperationException"> Thrown when attempting to add data while the sender is not active. </exception>
        /// <exception cref="ArgumentOutOfRangeException"> Thrown when the size of the binary data packet is too large to be sent. </exception>
        public void AddOscPacketToHeapTail(Packet packet, int priorityLevel = 0)
        {

            if (!_isActive)
            {
                throw new InvalidOperationException("SC Server Sender Error: Sender is not running.");
            }

            if (packet.Length > _packetMaxLength)
            {
                throw new ArgumentOutOfRangeException("OSC Sender Error: Too much OSC data to safely send in one message.");
            }
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }


            int priority;

            // make sure priority level is within bounds
            if (priorityLevel < 0)
            {
                priority = 0;
            }
            else if (priorityLevel >= (_packetHeapPriorityLayersTotal - 1))
            {
                priority = _packetHeapPriorityLayersTotal - 1;
            }
            else
            {
                priority = priorityLevel;
            }

            // finally, add data to heap
            try
            {
                _packetHeapAccessMutex.WaitOne();
                _packetHeap[priority].Add(packet);
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                _packetHeapAccessMutex.ReleaseMutex();
            }

        }

        #endregion


        #region TASKS AND INTERNALS
        private async Task HeapProcessingCycle()
        {

            while (_isActive)
            { 

                if (_oscLink.Mode == LinkMode.Targeted)
                {
                    // check if we got data to send
                    bool dataFound = false;
                    int highestPriority = (_packetHeapPriorityLayersTotal - 1);

                    for (int i = _packetHeapPriorityLayersTotal - 1; i >= 0; i--)
                    {
                        if (_packetHeap[i].Count > 0)
                        {
                            highestPriority = i;
                            dataFound = true;
                            break;
                        }

                    }

                    if (dataFound)
                    {
                        try
                        {
                            _packetHeapAccessMutex.WaitOne();

                            for (int i = 0; i <= highestPriority; i++)
                            {
                                try
                                {
                                    ProcessHeapLevel(i);
                                }
                                catch (Exception e)
                                {
                                    SendTaskExceptionRaised?.Invoke(e);
                                    
                                    // wipe the packet heap level, just in case
                                    _packetHeap[i].Clear();
                                }
                            }
                        }
                        finally
                        {
                            _packetHeapAccessMutex.ReleaseMutex();

                            await Task.Delay(_cycleLengthMs);
                        }

                    }
                    else
                    {
                        await Task.Delay(_cycleLengthMs);
                    }

                }
                else
                {
                    await Task.Delay(_cycleLengthMs);               
                }

            }

        }

        /// <summary>
        /// Processes one level of data heap and sends either bundles or messages according to the setting.
        /// </summary>
        /// <param name="priorityLevel"></param>
        private void ProcessHeapLevel(int priorityLevel)
        {
            // get reference to the current priority level for convenience
            List<Packet> packetHeapLevel = _packetHeap[priorityLevel];

            // total messages in the current data heap level that still needs to be looked at (not neceserally processed this time)
            int totalMessages = packetHeapLevel.Count;

            while (totalMessages > 0)
            {
                if (_bundlePacketsBeforeSending)
                {
                    // counts the total of bytes currently in a bundle
                    int byteCounter = 0;

                    byte[] currentPacketData;
                    int currentPacketLength;

                    for (int i = totalMessages - 1; i >= 0; i--)
                    {
                        if (_packetRemoverMethod(packetHeapLevel[i])) // let's check with the provided remover method delegate whether the message needs deleting and delete if true
                        {
                            packetHeapLevel.RemoveAt(i);

                            totalMessages--;
                        } 
                        else if (_packetReadyCheckerMethod(packetHeapLevel[i])) // otherwise, let's check if the data is eligible for sending. it always will be by default, but derived sender classes can change it
                        {
                            // cache stuff
                            currentPacketData = packetHeapLevel[i].BinaryData;
                            currentPacketLength = packetHeapLevel[i].Length;

                            // then let's check the message length in bytes, plus the byte size of message length itself (that's the magic "4") in case it's too long to be sent
                            if ((byteCounter + currentPacketLength + 4) < _packetMaxLength) 
                            {
                                // message will be processed and added to the bundle, let's account for that 
                                totalMessages--;

                                // add bytes of message's length to bundle and shift byte counter by a magical 4 (for bytes in int32)
                                OscSerializer.ArgumentToBinary(currentPacketLength).CopyTo(_cycleBinaryDataHolder, byteCounter);                  
                                byteCounter += 4;

                                // add message data to the data holder
                                currentPacketData.CopyTo(_cycleBinaryDataHolder, byteCounter);
                                byteCounter += currentPacketLength;

                                packetHeapLevel.RemoveAt(i);

                            }
                            else
                            {
                                // let's stop here and send what we got
                                break;
                            }

                        }
                        else
                        {
                            // if message is not yet ready to be sent due to its flags, count is as processed for this go-around
                            totalMessages--;
                        }
                                       
                    }

                    // if we got some bytes to send let's send them
                    if (byteCounter > 0)
                    {
                        // get the copy of array that is limited to the byteCounter
                        byte[] bundleData = new byte[byteCounter];

                        Array.Copy(_cycleBinaryDataHolder, 0, bundleData, 0, byteCounter);

                        OscPacketBytes newBundle = OscSerializer.BundleToBytes(bundleData);

                        _oscLink.SendToTarget(newBundle);
                                          
                    }

                }
                else
                {
                    // just send a message, or pass it if it's not yet time
                    totalMessages--;

                    if (_packetReadyCheckerMethod(packetHeapLevel[totalMessages]))
                    {
                        Packet message = packetHeapLevel[totalMessages];

                        _oscLink.SendToTarget(message);

                        packetHeapLevel.RemoveAt(totalMessages);

                    }
                    
                }

            }
     
        }

        // this is the default methods to feed into delegates
        private bool DefaultMessageChecker(Packet packet)
        {
            return true;
        }


        private bool DefaultMessageDeleter(Packet packet)
        {
            return false;
        }

        #endregion

        /// <summary>
        /// Returns an overview of the packet heap, showing the number of packets per priority level.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {

            StringBuilder stringBuilder = new StringBuilder();

            if (_isActive)
            {
                stringBuilder.Append("Sender status: ACTIVE\n");
                stringBuilder.Append("OSC Link: ");
                stringBuilder.Append(_oscLink.Name);
                stringBuilder.Append('\n');

                try
                {
                    _packetHeapAccessMutex.WaitOne();

                    for (int i = 0; i < _packetHeapPriorityLayersTotal; i++)
                    {
                        stringBuilder.Append("Packet heap priority level ");
                        stringBuilder.Append(i);
                        stringBuilder.Append(": total packets - ");
                        stringBuilder.Append(_packetHeap[i].Count);
                        stringBuilder.Append('\n');
                    }

                }
                finally
                {
                    _packetHeapAccessMutex.ReleaseMutex();
                }

            }
            else
            {
                stringBuilder.Append("Sender status: INACTIVE\n");
            }

            return stringBuilder.ToString();

        }

    }

}
