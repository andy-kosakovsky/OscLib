using System;
using System.Net;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OscLib
{
    /// <summary>
    ///  Processes binary data-containing OSC Packets received by the connected OSC Link, converts them into Bundles and Messages using the provided Converter. 
    /// </summary>
    /// <remarks> Can be configured to delay the invocation of incoming bundles according to their timetags. </remarks>
    public class OscReceiver
    {
        // TODO: add bundle delay thing here.
        /// <summary> The name of this OscReceiver. </summary>
        protected readonly string _name;

        /// <summary> The connected OscLink. </summary>
        protected OscLink _link;       

        /// <summary> The connected OscConverter. </summary>
        protected OscConverter _converter;


        /// <summary> Holds received OSC Bundles if their timetags demand delayed invocation (and if the Receiver is configured to do so). </summary>
        protected List<KeyValuePair<OscBundle, IPEndPoint>> _heap;

        /// <summary> Controls access to the bundle-holding heap.  </summary>
        protected Mutex _heapAccess;

        /// <summary> Periodically checks the contents of the bundle heap, invokes OSC Bundles when the time is right. </summary>
        protected Task _heapTask;


        /// <summary> How long the heap-checking task will wait between checks, in milliseconds. Minimum value is 1. </summary>
        protected int _cycleWaitMs;

        /// <summary> Whether to ignore incoming OSC Bundles' timetags and invoke them as they arrive, or keep them on the heap and invoke them when their timetags demand it. </summary>
        protected bool _ignoreTimetags;

        /// <summary> Whether this OSC Receiver is currently active - that is, connected to an OSC Link and receiving packets. </summary>
        protected bool _isActive;


        /// <summary> Whether to ignore incoming OSC Bundles' timetags and invoke them as they arrive, or keep them on the heap and invoke them when their timetags demand it. </summary>
        public bool IgnoreTimetags { get => _ignoreTimetags; set => _ignoreTimetags = value; }

        /// <summary> The length of time between checks on the stored OSC Bundles. Minimum value is 1. </summary>
        public int CycleWaitMs { get => _cycleWaitMs; set => _cycleWaitMs = value.Clamp(1, int.MaxValue); }


        #region EVENTS
        /// <summary> Invoked when the connected OSC Link receives a packet that contains an OSC Message. </summary>
        public MessageHandler MessageReceived;

        /// <summary> Invoked when the connected OSC Link receives a packet that contains an OSC Bundle. </summary>
        public BundleHandler BundleReceived;

        /// <summary> Invoked when an exception happens inside the heap-processing task, hopefully preventing it from stopping </summary>
        public OscTaskExceptionHandler HeapTaskExceptionRaised;

        #endregion // EVENTS


        /// <summary>
        /// Creates a new OSC Receiver, specifies its initial settings.
        /// </summary>
        /// <param name="name"> The name of this Receiver. </param>
        /// <param name="ignoreTimetags"> Whether to ignore the incoming OSC Bundles' timetags and invoke them as they come, or not. </param>
        /// <param name="cycleLengthMs"> The length of time between checks on stored OSC Bundles, in milliseconds. </param>
        public OscReceiver(string name, bool ignoreTimetags = false, int cycleLengthMs = 5)
        {
            _name = name;
            _ignoreTimetags = ignoreTimetags;
            CycleWaitMs = cycleLengthMs;

            _heapAccess = new Mutex();        
        }


        public void Connect(OscLink link, OscConverter converter)
        {
            if (_isActive)
            {
                return;
            }

            if (link == null)
            {
                throw new ArgumentNullException(nameof(link));
            }

            if (converter == null)
            {
                throw new ArgumentNullException(nameof(converter));
            }
 
            _link = link;
            _converter = converter;

            _link.PacketReceived += ReceivePacket;
            _heap = new List<KeyValuePair<OscBundle, IPEndPoint>>();

            _heapTask = Task.Run(HeapProcessingCycle);

            _isActive = true;

        }


        public void Disconnect()
        {
            if (!_isActive)
            {
                return;
            }

            _link.PacketReceived -= ReceivePacket;

            _isActive = false;
            
            _heapTask.Wait();

            _link = null;
            _converter = null;

        }


        /// <summary>
        /// Receives an OSC Packet and converts it into either a Message or a Bundle, using the connected Converter. 
        /// Depending on configuration and on the received Bundle's time tag, either invokes it straight away or holds it for the specified period of time.
        /// </summary>
        /// <typeparam name="TPacket"></typeparam>
        /// <param name="packet"></param>
        /// <param name="endPoint"></param>
        public virtual void ReceivePacket<TPacket>(TPacket packet, IPEndPoint endPoint) where TPacket : IOscPacket
        {
       
            if (packet.CheckOscContents() == PacketContents.Bundle)
            {
                if (_ignoreTimetags)
                {
                    // invoke straight away
                    BundleReceived?.Invoke(_converter.GetBundle(packet), endPoint);
                }
                else
                {
                    // extract all bundles within bundles into a flat array
                    OscBundle[] arrivals = _converter.GetBundles(packet.GetContents());

                    try
                    {
                        _heapAccess.WaitOne();

                        for (int i = 0; i < arrivals.Length; i++)
                        {
                            // if timetag is earlier than the current time, invoke right away
                            if (arrivals[i].Timetag.Ticks < OscTime.GlobalTick)
                            {
                                BundleReceived?.Invoke(arrivals[i], endPoint);
                            }
                            else
                            {                              
                                KeyValuePair<OscBundle, IPEndPoint> arrival = new KeyValuePair<OscBundle, IPEndPoint>(arrivals[i], endPoint);

                                // insert the new arrival right after the first bundle with a larger timetag. 
                                // this will automaticall sort the heap - the later the invocation time, the closer to the beginning the bundle will be kept.
                                if (_heap.Count > 0)
                                {
                                    for (int j = _heap.Count - 1; j >= 0; j--)
                                    {
                                        if (_heap[j].Key.Timetag > arrivals[i].Timetag)
                                        {
                                            if (j == _heap.Count - 1)
                                            {
                                                _heap.Add(arrival);
                                            }
                                            else
                                            {
                                                _heap.Insert(j + 1, arrival);
                                            }

                                            break;
                                        }

                                        if (j == 0)
                                        {
                                            _heap.Insert(0, arrival);
                                            break;
                                        }

                                    }

                                }
                                else
                                {
                                    _heap.Add(arrival);
                                }

                            }

                        }

                    }
                    finally
                    {
                        _heapAccess.ReleaseMutex();
                    }

                }

            }
            else if (packet.CheckOscContents() == PacketContents.Message)
            {
                MessageReceived?.Invoke(_converter.GetMessage(packet), endPoint);
            }         
            
        }


        /// <summary>
        /// The bundle heap processing task. Checks each bundle on the heap, starting from the last one and working backwards. Invokes a bundle if its timetag is earlier than the current GlobalTick. 
        /// </summary>
        /// <returns></returns>
        protected async Task HeapProcessingCycle()
        {
            while (_isActive)
            {
                if (_heap.Count > 0)
                {
                    try
                    {
                        _heapAccess.WaitOne();

                        for (int i = _heap.Count - 1; i >= 0; i--)
                        {
                            if (_heap[i].Key.Timetag.Ticks < OscTime.GlobalTick)
                            {
                                BundleReceived?.Invoke(_heap[i].Key, _heap[i].Value);
                                _heap.RemoveAt(i);
                            }
                            else
                            {
                                // bundles are stored in their timetags' descending order. if we get a timetag that is larger than the current tick, 
                                // we might as well stop right there and then.
                                break;
                            }

                        }

                    }
                    finally
                    {
                        _heapAccess.ReleaseMutex();
                    }

                }
                else
                {
                    await Task.Delay(_cycleWaitMs);
                }

            }

        }


        public override string ToString()
        {
            StringBuilder returnString = new StringBuilder("OscReveiver: name: ");
            returnString.Append(_name);

            if (_converter != null)
            {
                returnString.Append("; connected OscConverter type: ");
                returnString.Append(_converter.GetType().ToString());
            }

            if (_link != null)
            {
                returnString.Append("; connected OscLink: ");
                returnString.Append(_link.ToString());
            }

            if (!_ignoreTimetags)
            {
                returnString.Append("; ");
                returnString.Append(_heap.Count);
                returnString.Append(" bundles waiting on the heap: "); 
            }

            return returnString.ToString();

        }

    }

}
