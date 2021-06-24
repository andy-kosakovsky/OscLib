using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace OscLib
{
    /// <summary>
    /// Implements a multi-priority sender queue to work with an OSC Link, for message bundling and orderly sending. 
    /// 
    /// The methods deciding whether a packet on the heap is to be sent or discarded can be overriden, as is the timetag-providing method - it is used 
    /// if the Sender is configured to bundle packets before sending.
    /// 
    /// OSC Link needs to be in targeted mode when operating, otherwise nothing will get sent.
    /// </summary>
    /// <typeparam name="TPacket"> The particular type of the OSC Packet used with this Sender. Should implement the IOscPacket interface. </typeparam>
    public class OscPacketHeap<TPacket> where TPacket : IOscPacket
    {
        #region FIELDS
        /// <summary> OSC Link in use with this Sender. </summary>
        protected OscLink _link;

        /// <summary> The total amount of priority levels in this Packet Heap. </summary>
        protected int _heapTotalLayers;


        /// <summary> The packet heap, holding all the packets before they are processed and sent (or not). Implements several priority levels. </summary>
        protected List<TPacket>[] _heap;

        /// <summary> Used to facilitate orderly access to the packet heap. </summary>
        protected Mutex _heapAccess;

        /// <summary> Periodically checks the contents of the packet heap and manages it, sending the packets out when necessary. </summary>
        protected Task _heapTask;

        /// <summary> The time between heap checks, in milliseconds. Shoudln't be less than 1. </summary>
        protected int _cycleWaitMs;


        /// <summary> Used to cache packet binary data  </summary>
        protected byte[] _cycleDataHolder;

        /// <summary> Maximum amount of data sent per packet, in bytes. As per UDP spec, sending more than 508 bytes per packet is not advisable. </summary> 
        protected int _packetBundleMaxSize;

        /// <summary> Should this Packet Heap bundle the packets before sending, or just send them as separate messages as they come. </summary>
        protected bool _bundlePacketsBeforeSending;

        /// <summary> Whether this Sender is currently active. </summary>
        protected bool _isActive;

        #endregion


        #region PROPERTIES
        /// <summary> Whether this Sender is currently active. </summary>
        public bool IsActive { get => _isActive; }

        /// <summary> The total amount of priority levels in this Sender's packet heap. </summary>
        public int TotalLayers { get => _heapTotalLayers; }

        /// <summary> Shows the current status of the task processing the packet heap. </summary>
        public TaskStatus TaskStatus { get => _heapTask.Status; }
      
        /// <summary> Should this Sender bundle the packets before sending, or just send them as separate messages as they come. </summary>
        public bool BundlePacketsBeforeSending { get => _bundlePacketsBeforeSending; set => _bundlePacketsBeforeSending = value; }

        /// <summary> The length of the heap check cycle of this Sender - that is, the time between heap checks, in milliseconds. Minimum 1 ms. </summary>
        public int CycleLengthMs 
        { 
            get => _cycleWaitMs;  
            
            set
            {
                if (value > 1)
                    _cycleWaitMs = value;
                else
                    _cycleWaitMs = 1;
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
                        _heapAccess.WaitOne();
                        _packetBundleMaxSize = newLength;
                        _cycleDataHolder = new byte[_packetBundleMaxSize];
                    }
                    finally
                    {
                        _heapAccess.ReleaseMutex();
                    }

                }

            }

        }

        #endregion


        #region EVENTS
        /// <summary> Invoked when an exception happens inside the heap-processing task, hopefully preventing it from stopping </summary>
        public event OscTaskExceptionHandler HeapTaskExceptionRaised;

        #endregion


        #region CONSTRUCTORS

        /// <summary>
        /// Creates a new OSC Packet Heap.
        /// </summary>
        /// <param name="packetBundleMaxSize"> Maximum amount of data sent per packet when bundling packets, in bytes. As per UDP spec, sending more than 508 bytes per packet is not advisable. </param>
        /// <param name="cycleLengthMs"> The length of the heap check cycle of this Sender - that is, the time between heap checks, in milliseconds. Shoudln't be less than 1. </param>
        /// <param name="packetHeapTotalLayers"> The total amount of priority levels in this Sender's packet heap. </param>
        /// <param name="bundlePacketsBefureSending"> Should this Sender bundle the packets before sending, or just send them as separate messages as they come. </param>
        public OscPacketHeap(int packetBundleMaxSize = 508, int cycleLengthMs = 1, int packetHeapTotalLayers = 1, bool bundlePacketsBefureSending = true)
        {
            _heapAccess = new Mutex();
            _packetBundleMaxSize = packetBundleMaxSize;
            _cycleDataHolder = new byte[_packetBundleMaxSize];

            // set options
            _heapTotalLayers = packetHeapTotalLayers.Clamp(1, int.MaxValue);

            _cycleWaitMs = cycleLengthMs;
            _bundlePacketsBeforeSending = bundlePacketsBefureSending;

        }

        #endregion

        #region SYSTEM32

        /// <summary>
        /// Activates the Heap and connects it to an OSC Link.
        /// The packet heap is wiped on every activation.
        /// </summary>
        /// <param name="oscLink"> The OSC Link this Sender will be sending packets through. </param>
        /// <exception cref="ArgumentNullException"> Is thrown when the provided OSC Link is null. </exception>
        public void Connect(OscLink oscLink)
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
            _link = oscLink;

            // initialize data heap with requested number of priority levels
            _heap = new List<TPacket>[_heapTotalLayers];

            for (int i = 0; i < _heapTotalLayers; i++)
            {
                _heap[i] = new List<TPacket>();
            }

            _heapTask = Task.Run(HeapProcessingCycle);

            _isActive = true;

        }


        /// <summary>
        /// Deactivates the Sender and disconnects it from the OSC Link.
        /// </summary>
        public void Disconnect()
        {
            if (!_isActive)
            {
                return;
            }
            
            _isActive = false;
            _heapTask.Wait();
            _link = null;

        }

        #endregion // SYSTEM32


        #region SENDING PACKETS

        /// <summary>
        /// Gets the binary data from the OSC packet and immediately sends it through the OSC Link.
        /// </summary>
        /// <param name="packet"> A reference to the OSC binary data packet to be sent. </param>
        public void SendPacket(TPacket packet)
        {
            if (_link.Mode == LinkMode.ToTarget)
            {
                _link.SendToTarget(packet);
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
        public void AddPacketToHeapHead(TPacket packet, int priorityLevel = 0)
        {
            if (!_isActive)
            {
                throw new InvalidOperationException("SC Server Sender Error: Sender is not active.");
            }

            if (packet.Size > _packetBundleMaxSize)
            {
                throw new ArgumentOutOfRangeException(nameof(packet), "OSC Sender Error: Too much OSC data to safely send in one message.");
            }

            int priority = priorityLevel.Clamp(0, _heap.Length);

            // finally, add data to heap
            try
            {
                _heapAccess.WaitOne();
                _heap[priority].Insert(0, packet);
            }
            finally
            {
                _heapAccess.ReleaseMutex();
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
        public void AddPacketToHeapTail(TPacket packet, int priorityLevel = 0)
        {

            if (!_isActive)
            {
                throw new InvalidOperationException("SC Server Sender Error: Sender is not running.");
            }

            if (packet.Size > _packetBundleMaxSize)
            {
                throw new ArgumentOutOfRangeException("OSC Sender Error: Too much OSC data to safely send in one message.");
            }
            if (packet == null)
            {
                throw new ArgumentNullException(nameof(packet));
            }

            // make sure priority level is within bounds
            int priority = priorityLevel.Clamp(0, _heap.Length - 1);
            
            // finally, add data to heap
            try
            {
                _heapAccess.WaitOne();
                _heap[priority].Add(packet);
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                _heapAccess.ReleaseMutex();
            }

        }

        #endregion // SENDING PACKETS



        #region TASKS AND INTERNALS
        protected virtual async Task HeapProcessingCycle()
        {

            while (_isActive)
            { 

                if (_link.Mode == LinkMode.ToTarget)
                {
                    // check if we got data to send
                    bool dataFound = false;
                    int highestPriority = _heapTotalLayers - 1;

                    for (int i = _heapTotalLayers - 1; i >= 0; i--)
                    {
                        if (_heap[i].Count > 0)
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
                            _heapAccess.WaitOne();

                            for (int i = 0; i <= highestPriority; i++)
                            {
                                try
                                {
                                    ProcessHeapLevel(i);
                                }
                                catch (Exception e)
                                {
                                    HeapTaskExceptionRaised?.Invoke(e);
                                    
                                    // wipe the packet heap level, just in case
                                    _heap[i].Clear();
                                }

                            }

                        }
                        finally
                        {
                            _heapAccess.ReleaseMutex();

                            await Task.Delay(_cycleWaitMs);
                        }

                    }
                    else
                    {
                        await Task.Delay(_cycleWaitMs);
                    }

                }
                else
                {
                    await Task.Delay(_cycleWaitMs);               
                }

            }

        }

        /// <summary>
        /// Processes one level of data heap and sends either bundles or messages according to the setting.
        /// </summary>
        /// <param name="priorityLevel"></param>
        protected virtual void ProcessHeapLevel(int priorityLevel)
        {
            // get reference to the current priority level for convenience
            List<TPacket> packetHeapLevel = _heap[priorityLevel];

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
                        if (ShouldRemovePacket(packetHeapLevel[i])) // let's check with the provided remover method delegate whether the message needs deleting and delete if true
                        {
                            packetHeapLevel.RemoveAt(i);

                            totalPackets--;
                        } 
                        else if (ShouldSendPacket(packetHeapLevel[i])) // otherwise, let's check if the data is eligible for sending
                        {
                            // cache stuff
                            packetData = packetHeapLevel[i].GetContents();
                            packetLength = packetHeapLevel[i].Size;

                            // check whether this packet will fit (
                            if ((byteCounter + packetLength + OscBundle.BundleHeaderLength) < _packetBundleMaxSize) 
                            {
                                // message will be processed and added to the bundle, let's account for that 
                                totalPackets--;

                                // add bytes of message's length to bundle and shift byte counter by a magical 4 (for bytes in int32)
                                OscSerializer.AddBytes(packetLength, _cycleDataHolder, ref byteCounter);
                                
                                // add message data to the data holder
                                packetData.CopyTo(_cycleDataHolder, byteCounter);
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
                        OscConverter.AddBundleHeader(_cycleDataHolder, 0, GetTimetag());
                        
                        OscPacket newBundle = new OscPacket(_cycleDataHolder);

                        _link.SendToTarget(newBundle, byteCounter);
                                          
                    }

                }
                else
                {
                    // just send the packet or remove it, or pass it if it's not yet time
                    totalPackets--;

                    if (ShouldSendPacket(packetHeapLevel[totalPackets]))
                    {
                        TPacket message = packetHeapLevel[totalPackets];

                        _link.SendToTarget(message);

                        packetHeapLevel.RemoveAt(totalPackets);

                    }
                    else if (ShouldRemovePacket(packetHeapLevel[totalPackets]))
                    {
                        packetHeapLevel.RemoveAt(totalPackets);
                    }
                    
                }

            }
     
        }


        protected virtual bool ShouldSendPacket(TPacket packet)
        {
            return true;
        }


        protected virtual bool ShouldRemovePacket(TPacket packet)
        {
            return false;
        }


        protected virtual OscTimetag GetTimetag()
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
                stringBuilder.Append(_link.Name);
                stringBuilder.Append('\n');

                try
                {
                    _heapAccess.WaitOne();

                    for (int i = 0; i < _heapTotalLayers; i++)
                    {
                        stringBuilder.Append("Packet heap priority level ");
                        stringBuilder.Append(i);
                        stringBuilder.Append(": total packets - ");
                        stringBuilder.Append(_heap[i].Count);
                        stringBuilder.Append('\n');
                    }

                }
                finally
                {
                    _heapAccess.ReleaseMutex();
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
