using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace OscLib
{
    /// <summary>
    /// Implements a multi-priority sender queue to work with an OSC Link, for message bundling and orderly sending. OSC Link needs to be in targeted mode when operating, otherwise nothing will get sent.
    /// </summary>
    /// <typeparam name="Packet"> The particular type of the OSC Packet used with this Sender. Should implement the IOscPacket interface. </typeparam>
    public class OscSender<Packet> where Packet : IOscPacket
    {
        #region FIELDS
        /// <summary> OSC Link in use with this Sender. </summary>
        protected OscLink _oscLink;

        /// <summary> The total amount of priority levels in this Sender's packet heap. </summary>
        protected int _packetHeapTotalLayers;

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
        protected int _packetBundleMaxSize;

        /// <summary> The Task that performs the packet heap checks and manages it, sending the packets out when necessary. </summary>
        protected Task _packetHeapTask;

        // delegates
        /// <summary> Checks whether the packet should be sent in the current cycle. </summary> 
        protected OscPacketHeapCheck<Packet> _shouldSendPacket;

        /// <summary> Checks whether the packet should be removed in the current cycle. </summary> 
        protected OscPacketHeapCheck<Packet> _shouldRemovePacket;

        /// <summary> Provides timetags for the bundles this sender sends out. </summary>
        protected OscTimetagSource _timetagSource;

        #endregion


        #region PROPERTIES
        /// <summary> Whether this Sender is currently active. </summary>
        public bool IsActive { get => _isActive; }

        public int PacketHeapTotalLayers { get => _packetHeapTotalLayers; }

        /// <summary> Shows the current status of the task processing the packet heap. </summary>
        public TaskStatus PacketHeapTaskStatus { get => _packetHeapTask.Status; }
      
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
            get => _packetBundleMaxSize;
            set
            {
                int newLength = value;

                // ensure the minimum
                if (newLength < 128)
                {
                    newLength = 128;
                }

                if (newLength != _packetBundleMaxSize)
                {
                    try
                    {
                        // just to be safe, let's wait until cycle bundle data holder is not in use
                        _packetHeapAccessMutex.WaitOne();
                        _packetBundleMaxSize = newLength;
                        _cycleBinaryDataHolder = new byte[_packetBundleMaxSize];
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
        /// <param name="packetBundleMaxSize"> Maximum amount of data sent per packet when bundling packets, in bytes. As per UDP spec, sending more than 508 bytes per packet is not advisable. </param>
        /// <param name="cycleLengthMs"> The length of the heap check cycle of this Sender - that is, the time between heap checks, in milliseconds. Shoudln't be less than 1. </param>
        /// <param name="packetHeapTotalLayers"> The total amount of priority levels in this Sender's packet heap. </param>
        /// <param name="bundlePacketsBefureSending"> Should this Sender bundle the packets before sending, or just send them as separate messages as they come. </param>
        public OscSender(int packetBundleMaxSize = 508, int cycleLengthMs = 1, int packetHeapTotalLayers = 1, bool bundlePacketsBefureSending = true)
        {
            _packetHeapAccessMutex = new Mutex();
            _packetBundleMaxSize = packetBundleMaxSize;
            _cycleBinaryDataHolder = new byte[_packetBundleMaxSize];

            // get default methods to act as delegates
            _shouldSendPacket = DefaultShouldSendPacket;
            _shouldRemovePacket = DefaultShouldRemovePacket;
            _timetagSource = DefaultTimetagSource;

            // set options
            _packetHeapTotalLayers = OscUtil.Clamp(packetHeapTotalLayers, 1, int.MaxValue);

            _cycleLengthMs = cycleLengthMs;
            _bundlePacketsBeforeSending = bundlePacketsBefureSending;

        }

        #endregion

        #region SYSTEM32

        /// <summary>
        /// Activates the Sender and connects it to an OSC Link. 
        /// </summary>
        /// <param name="oscLink"> The OSC Link this Sender will be sending packets through. </param>
        /// <exception cref="ArgumentNullException"> Is thrown when the provided OSC Link is null. </exception>
        public void Activate(OscLink oscLink)
        {
            
            if (oscLink == null)
            {
                throw new ArgumentNullException(nameof(oscLink));
            }

            if (_isActive)
            {
                return;
            }

            // get the link
            _oscLink = oscLink;

            // initialize data heap with requested number of priority levels
            _packetHeap = new List<Packet>[_packetHeapTotalLayers];

            for (int i = 0; i < _packetHeapTotalLayers; i++)
            {
                _packetHeap[i] = new List<Packet>();
            }

            _isActive = true;

            _packetHeapTask = Task.Run(HeapProcessingCycle);

        }


        /// <summary>
        /// Deactivates the sender, cancelling the heap processing task and detatching the sender from the OSC Link.
        /// </summary>
        public void Deactivate()
        {
            if (!_isActive)
            {
                return;
            }
            
            _isActive = false;
            _packetHeapTask.Wait();
            _oscLink = null;

        }


        /// <summary>
        /// Provides the method delegate for checking if a packet in the heap is eligible to be sent.
        /// </summary>
        /// <param name="shouldSendPacketMethod"> The method to be used for checking packets. </param>
        public void SetMethodShouldSentPacket(OscPacketHeapCheck<Packet> shouldSendPacketMethod)
        {
            _shouldSendPacket = shouldSendPacketMethod;
        }


        /// <summary>
        /// Provides the method delegate for checking if a packet needs to be removed from the heap.
        /// </summary>
        /// <param name="shouldRemovePacketMethod"> The method to be used for removing packets. </param>
        public void SetMethodShouldRemovePacket(OscPacketHeapCheck<Packet> shouldRemovePacketMethod)
        {
            _shouldRemovePacket = shouldRemovePacketMethod;
        }

        public void SetMethodTimetagSource(OscTimetagSource timetagSourceMethod)
        {
            _timetagSource = timetagSourceMethod;
        }

        #endregion // SYSTEM32


        #region SENDING PACKETS

        /// <summary>
        /// Gets the binary data from the OSC packet and immediately sends it through the OSC Link.
        /// </summary>
        /// <param name="packet"> A reference to the OSC binary data packet to be sent. </param>
        public void SendPacket(Packet packet)
        {
            if (_oscLink.Mode == LinkMode.Targeted)
            {
                _oscLink.SendToTarget(packet);
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
        public void AddPacketToHeapHead(Packet packet, int priorityLevel = 0)
        {
            if (!_isActive)
            {
                throw new InvalidOperationException("SC Server Sender Error: Sender is not active.");
            }

            if (packet.OscLength > _packetBundleMaxSize)
            {
                throw new ArgumentOutOfRangeException(nameof(packet), "OSC Sender Error: Too much OSC data to safely send in one message.");
            }

            int priority = OscUtil.Clamp(priorityLevel, 0, _packetHeap.Length);

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
        /// Adds the OSC packet to the end of the heap at the specified priority level, passing the packet directly.
        /// <para> Important to note: heap levels are processed tail-to-head, so the packet added by this method will be first in line.</para>
        /// <para> This method is faster than adding to heap head. </para>
        /// </summary>        
        /// <param name="packet"> The OSC binary data packet. </param>
        /// <param name="priorityLevel"> The priority level of the packet, with 0 being the highest priority. </param>
        /// <exception cref="InvalidOperationException"> Thrown when attempting to add data while the sender is not active. </exception>
        /// <exception cref="ArgumentOutOfRangeException"> Thrown when the size of the binary data packet is too large to be sent. </exception>
        public void AddPacketToHeapTail(Packet packet, int priorityLevel = 0)
        {

            if (!_isActive)
            {
                throw new InvalidOperationException("SC Server Sender Error: Sender is not running.");
            }

            if (packet.OscLength > _packetBundleMaxSize)
            {
                throw new ArgumentOutOfRangeException("OSC Sender Error: Too much OSC data to safely send in one message.");
            }
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            // make sure priority level is within bounds
            int priority = OscUtil.Clamp(priorityLevel, 0, _packetHeap.Length - 1);
            
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

        #endregion // SENDING PACKETS


        #region SENDING MESSAGES
        public void SendMessage(OscMessage message)
        {
            if (_oscLink.Mode == LinkMode.Targeted)
            {
                _oscLink.SendToTarget(message);
            }

        }


        #endregion // SENDING MESSAGES

        #region TASKS AND INTERNALS
        private async Task HeapProcessingCycle()
        {

            while (_isActive)
            { 

                if (_oscLink.Mode == LinkMode.Targeted)
                {
                    // check if we got data to send
                    bool dataFound = false;
                    int highestPriority = _packetHeapTotalLayers - 1;

                    for (int i = _packetHeapTotalLayers - 1; i >= 0; i--)
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
                                    throw e;

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
            int totalPackets = packetHeapLevel.Count;

            while (totalPackets > 0)
            {
                if (_bundlePacketsBeforeSending)
                {
                    // counts the total of bytes currently in the data holder. set to the length of bundle header in case we got something to send
                    int byteCounter = OscBundle.BundleHeaderLength;

                    // convenience "caches"
                    byte[] packetData;
                    int packetLength;

                    for (int i = totalPackets - 1; i >= 0; i--)
                    {
                        if (_shouldRemovePacket(packetHeapLevel[i])) // let's check with the provided remover method delegate whether the message needs deleting and delete if true
                        {
                            packetHeapLevel.RemoveAt(i);

                            totalPackets--;
                        } 
                        else if (_shouldSendPacket(packetHeapLevel[i])) // otherwise, let's check if the data is eligible for sending
                        {
                            // cache stuff
                            packetData = packetHeapLevel[i].BinaryData;
                            packetLength = packetHeapLevel[i].OscLength;

                            // check whether this packet will fit (
                            if ((byteCounter + packetLength + OscBundle.BundleHeaderLength) < _packetBundleMaxSize) 
                            {
                                // message will be processed and added to the bundle, let's account for that 
                                totalPackets--;

                                // add bytes of message's length to bundle and shift byte counter by a magical 4 (for bytes in int32)
                                OscSerializer.AddBytes(packetLength, _cycleBinaryDataHolder, ref byteCounter);
                                
                                // add message data to the data holder
                                packetData.CopyTo(_cycleBinaryDataHolder, byteCounter);
                                byteCounter += packetLength;

                                packetHeapLevel.RemoveAt(i);

                            }
                            else
                            {
                                // if we're going over the maximum length of the packet, it's Time to Stop
                                break;
                            }

                        }
                        else
                        {
                            // if message is not yet ready to be sent due to its flags, count is as processed for the current go-around
                            totalPackets--;
                        }
                                       
                    }

                    // if we got bytes to send beyond the standard bundle header let's send them
                    if (byteCounter > OscBundle.BundleHeaderLength)
                    {
                        // add bundle header
                        OscConvert.AddBundleHeader(_cycleBinaryDataHolder, 0, _timetagSource.Invoke());
                        
                        OscPacket newBundle = new OscPacket(_cycleBinaryDataHolder);

                        _oscLink.SendToTarget(newBundle, byteCounter);
                                          
                    }

                }
                else
                {
                    // just send the packet, or pass it if it's not yet time
                    totalPackets--;

                    if (_shouldSendPacket(packetHeapLevel[totalPackets]))
                    {
                        Packet message = packetHeapLevel[totalPackets];

                        _oscLink.SendToTarget(message);

                        packetHeapLevel.RemoveAt(totalPackets);

                    }
                    else if (_shouldRemovePacket(packetHeapLevel[totalPackets]))
                    {
                        packetHeapLevel.RemoveAt(totalPackets);
                    }
                    
                }

            }
     
        }

        // this is the default methods to feed into delegates
        protected virtual bool DefaultShouldSendPacket(Packet packet)
        {
            return true;
        }


        protected virtual bool DefaultShouldRemovePacket(Packet packet)
        {
            return false;
        }


        protected virtual OscTimetag DefaultTimetagSource()
        {
            return OscTime.Immediately;
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

                    for (int i = 0; i < _packetHeapTotalLayers; i++)
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
