using System;
using System.Net;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OscLib
{
    public class OscReceiver
    {
        // TODO: add bundle delay thing here.
        protected readonly string _name;

        protected OscLink _link;       
        protected OscConverter _converter;

        protected List<KeyValuePair<OscBundle, IPEndPoint>> _heap;
        protected Mutex _heapAccess;
        protected Task _heapTask;
        protected int _cycleLengthMs;

        protected bool _isActive;
        protected bool _ignoreTimetags;


        public bool IgnoreTimetags { get => _ignoreTimetags; set => _ignoreTimetags = value; }


        #region EVENTS

        public MessageHandler MessageReceived;

        public BundleHandler BundleReceived;

        public BadDataHandler BadDataReceived;

        #endregion // EVENTS
        public OscReceiver(string name, bool ignoreTimetags = false, int cycleLengthMs = 20)
        {
            _name = name;
            _heapAccess = new Mutex();
            _cycleLengthMs = cycleLengthMs;
        }


        public void Connect(OscLink link, OscConverter converter)
        {
            if (_isActive)
            {
                return;
            }
             
            _link = link;
            _converter = converter;

            _link.PacketReceived += ReceivePacket;
            _heap = new List<KeyValuePair<OscBundle, IPEndPoint>>(64);

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


        protected virtual void ReceivePacket<Packet>(Packet packet, IPEndPoint endPoint) where Packet : IOscPacket
        {
       
            if (packet[0] == OscProtocol.BundleMarker)
            {
                if (_ignoreTimetags)
                {
                    BundleReceived?.Invoke(_converter.GetBundle(packet), endPoint);
                }
                else
                {
                    OscBundle[] arrivals = _converter.GetBundles(packet.GetBytes());

                    for (int i = 0; i < arrivals.Length; i++)
                    {
                        if (arrivals[i].Timetag.Ticks < OscTime.GlobalTick)
                        {
                            BundleReceived?.Invoke(arrivals[i], endPoint);
                        }
                        else
                        {
                            try
                            {
                                _heapAccess.WaitOne();
                                _heap.Add(new KeyValuePair<OscBundle, IPEndPoint>(arrivals[i], endPoint));
                            }
                            finally
                            {
                                _heapAccess.ReleaseMutex();
                            }

                        }

                    }

                }

            }
            else if (packet[0] == OscProtocol.Separator)
            {
                MessageReceived?.Invoke(_converter.GetMessage(packet), endPoint);
            }
            else
            {
                BadDataReceived?.Invoke(packet.GetBytes(), endPoint);
            }
            
        }


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
                            if (_heap[i].Key.Timetag.Ticks > OscTime.GlobalTick)
                            {
                                BundleReceived?.Invoke(_heap[i].Key, _heap[i].Value);
                                _heap.RemoveAt(i);
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
                    await Task.Delay(_cycleLengthMs);
                }

            }

        }

    }

}
